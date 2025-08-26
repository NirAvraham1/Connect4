using ConnectFourClient.Api;
using ConnectFourClient.LocalReplay;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;   // for smooth circles
using System.Linq;                
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;           // for loading user info by identifier
using Newtonsoft.Json;

namespace ConnectFourClient
{
    public partial class Form1 : Form
    {
        // ---- Board config ----
        private const int Rows = 6;
        private const int Cols = 7;
        private const int CellSize = 70;       // pixels
        private const int BoardMargin = 8;     // outer margin
        private const int PiecePadding = 10;   // inner padding for the disc inside the cell
        private const int InfoPanelWidth = 240; // right-side info panel width

        // ---- Animation ----
        private const int PixelsPerTick = 24;          // live falling speed (px per 16ms tick)
        private const int ReplayMoveDurationMs = 350;  // equal time per move in replay

        // ---- State ----
        private readonly ApiClient api = new ApiClient("https://localhost:7250");
        private static readonly HttpClient _http = new HttpClient { BaseAddress = new Uri("https://localhost:7250/") }; // keep in sync with ApiClient base URL

        private readonly int[,] board = new int[Rows, Cols];    // 0 empty, 1 player, 2 server
        private readonly int[] heights = new int[Cols];         // landed discs per column
        private readonly Rectangle[,] cellRects = new Rectangle[Rows, Cols];
        private readonly Rectangle boardRect;

        private readonly int identifier; // user Identifier (not DB Id)
        private int gameId = 0;
        private bool isGameOver = false;
        private string pendingResult = ""; // "InProgress" | "PlayerWin" | "ComputerWin" | "Draw"

        // ---- Local replay (LINQ to SQL) ----
        private LocalReplayRepository _replayRepo;
        private int _replaySessionId;
        private int _moveIndex;

        // Prevents double-ending local recording
        private bool _localRecordingEnded = true;

        // Replay flags
        private bool isReplaying = false;       // true when playing back from local DB
        private string _replayFinalResult = ""; // for end-of-replay message

         
        // ---- Animation item ----
        private class Anim
        {
            public int Player;     // 1=human, 2=server
            public int Column;     // 0..6
            public int TargetRow;  // 0..5
            public int X;          // pixel X (left of disc rect)
            public int Y;          // current pixel Y (top of disc rect)
            public int TargetY;    // pixel Y where it should stop
            public int Size;       // disc width/height (square) in pixels
            public double StepY;   // pixels per tick (computed for equal interval in replay)
        }

        private readonly Queue<Anim> animQueue = new Queue<Anim>();
        private readonly Timer animTimer = new Timer();

        // ---- UI ----
        private Button btnLocalReplays;

        // Right-side player info panel & labels
        private Panel _infoPanel;
        private Label _lblTitle;
        private Label _lblNameCaption, _lblNameValue;
        private Label _lblIdCaption, _lblIdValue;
        private Label _lblCountryCaption, _lblCountryValue;

        // Launch flags
        private readonly bool _startLiveGame;

        public Form1(int identifierFromArgs, bool openLocalReplays = false, bool startLiveGame = true)
        {
            if (identifierFromArgs <= 0)
            {
                MessageBox.Show("Missing identifier. Please launch the game from the website (Play).",
                                "Connect Four", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Environment.Exit(0);
                return;
            }
            identifier = identifierFromArgs;
            _startLiveGame = startLiveGame;

            InitializeComponent();

            // smoother drawing
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;
            this.Text = "Connect Four - Client";

            // compute board rect and window size
            int boardWidth = Cols * CellSize;
            int boardHeight = Rows * CellSize;
            boardRect = new Rectangle(BoardMargin, BoardMargin, boardWidth, boardHeight);

            // make room for the right info panel
            this.Width = boardRect.Right + BoardMargin + InfoPanelWidth + 24;
            this.Height = boardRect.Bottom + BoardMargin + 90;

            // precompute cell rectangles
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    int x = BoardMargin + c * CellSize;
                    int y = BoardMargin + r * CellSize;
                    cellRects[r, c] = new Rectangle(x, y, CellSize, CellSize);
                }
            }

            // events
            this.Load += async (s, e) => await OnFirstLoadAsync(openLocalReplays);
            this.MouseClick += Form1_MouseClick;
            this.Paint += Form1_Paint;

            // timer setup (~60 FPS)
            animTimer.Interval = 300;
            animTimer.Tick += AnimTimer_Tick;

            // Local Replays button (opens picker)
            btnLocalReplays = new Button
            {
                Text = "Local Replays…",
                AutoSize = true,
                Location = new Point(BoardMargin, boardRect.Bottom + 40)
            };
            btnLocalReplays.Click += (s, e) => OpenLocalReplays();
            this.Controls.Add(btnLocalReplays);

            // Right-side player info panel
            InitializeUserInfoPanel();
        }

        private async Task OnFirstLoadAsync(bool openLocal)
        {
            ResetBoardUI();
            pendingResult = "";
            isGameOver = false;
            isReplaying = false;
            _replayFinalResult = "";
            gameId = 0;

            _replayRepo = new LocalReplayRepository();

            // Show identifier immediately; load name/country async
            UpdateUserInfoPanel(null, identifier, null);
            _ = LoadUserInfoForIdentifierAsync(identifier);

            if (_startLiveGame)
            {
                try
                {
                    gameId = await api.StartGameByIdentifierAsync(identifier);
                    StartLocalRecording(identifier, gameId);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to start game: " + ex.Message, "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (openLocal)
            {
                // let the form show first
                this.BeginInvoke(new Action(OpenLocalReplays));
            }
        }

        // ===== Live game lifecycle =====
        private async void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            // In replay-only or no live game started – ignore mouse
            if (!_startLiveGame || isReplaying || isGameOver || gameId == 0) return;
            if (animQueue.Count > 0 || animTimer.Enabled) return;
            if (!boardRect.Contains(e.Location)) return;

            int col = (e.X - boardRect.X) / CellSize;
            if (col < 0 || col >= Cols) return;

            try
            {
                var resp = await api.PlayerMoveAsync(gameId, col);

                if (resp?.player != null)
                    EnqueueAnim(1, resp.player.Column, resp.player.Row);

                if (resp?.server != null)
                    EnqueueAnim(2, resp.server.Column, resp.server.Row);

                // Keep raw server result for UI message and any other messages
                pendingResult = (!string.IsNullOrEmpty(resp?.result))
                    ? resp.result
                    : "InProgress";

                if (!animTimer.Enabled) animTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to play: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetBoardUI()
        {
            Array.Clear(board, 0, board.Length);
            Array.Clear(heights, 0, heights.Length);
            animQueue.Clear();
            animTimer.Stop();
            Invalidate();
        }

        // ===== Animation helpers =====
        private void EnqueueAnim(int player, int col, int targetRow)
        {
            int discSize = CellSize - 2 * PiecePadding;
            int x = BoardMargin + targetRow * CellSize + PiecePadding;
            int startY = BoardMargin - discSize; // start above the board
            int targetY = BoardMargin + col * CellSize + PiecePadding;

            var a = new Anim
            {
                Player = player,
                Column = col,
                TargetRow = targetRow,
                X = startY,
                Y = x,
                TargetY = targetY,
                Size = discSize
            };

            if (isReplaying)
            {
                int distance = Math.Max(1, targetY - startY);
                int ticks = Math.Max(1, ReplayMoveDurationMs / Math.Max(1, animTimer.Interval));
                a.StepY = (double)distance / ticks;
            }
            else
            {
                a.StepY = PixelsPerTick;
            }

            animQueue.Enqueue(a);
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            if (animQueue.Count == 0)
            {
                animTimer.Stop();

                if (isReplaying)
                {
                    isReplaying = false;
                    string msg = !string.IsNullOrEmpty(_replayFinalResult)
                                 ? ("Replay finished. Result: " + _replayFinalResult)
                                 : "Replay finished.";
                    MessageBox.Show(msg, "Replay", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!string.IsNullOrEmpty(pendingResult) && !pendingResult.Equals("InProgress", StringComparison.OrdinalIgnoreCase))
                {
                    isGameOver = true;

                    // Normalize only for local DB storage
                    var normalized = MapResult(pendingResult);
                    FinishLocalRecording(normalized);

                    // For the user, show the raw server result
                    MessageBox.Show(pendingResult, "Game Over",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            var a = animQueue.Peek();
            int step = (int)Math.Max(1.0, Math.Round(a.StepY));
            a.Y += step;

            if (a.Y >= a.TargetY)
            {
                a.Y = a.TargetY;
                board[a.TargetRow, a.Column] = a.Player;
                heights[a.Column] = Math.Min(Rows, heights[a.Column] + 1);

                // Record locally only in live game
                if (_startLiveGame && !isReplaying)
                    RecordMove(a.Column, a.TargetRow, a.Player);

                animQueue.Dequeue();
            }

            Invalidate();
        }

        // ===== Painting =====
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var boardBrush = new SolidBrush(Color.SteelBlue))
            using (var borderPen = new Pen(Color.DimGray, 2))
            {
                g.FillRectangle(boardBrush, boardRect);
                g.DrawRectangle(borderPen, boardRect);
            }

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var cell = cellRects[r, c];
                    using (var cellBorderPen = new Pen(Color.Black, 1))
                        g.DrawRectangle(cellBorderPen, cell);

                    var circleRect = Rectangle.Inflate(cell, -PiecePadding, -PiecePadding);

                    int v = board[r, c];
                    if (v == 0)
                    {
                        using (var b = new SolidBrush(Color.WhiteSmoke))
                        using (var p = new Pen(Color.Gray, 1))
                        { g.FillEllipse(b, circleRect); g.DrawEllipse(p, circleRect); }
                    }
                    else if (v == 1)
                    {
                        using (var b = new SolidBrush(Color.Firebrick))
                        using (var p = new Pen(Color.Maroon, 1))
                        { g.FillEllipse(b, circleRect); g.DrawEllipse(p, circleRect); }
                    }
                    else // v == 2
                    {
                        using (var b = new SolidBrush(Color.Goldenrod))
                        using (var p = new Pen(Color.DarkGoldenrod, 1))
                        { g.FillEllipse(b, circleRect); g.DrawEllipse(p, circleRect); }
                    }
                }
            }

            if (animQueue.Count > 0)
            {
                var a = animQueue.Peek();
                var fallRect = new Rectangle(a.X, a.Y, a.Size, a.Size);

                if (a.Player == 1)
                {
                    using (var b = new SolidBrush(Color.Firebrick))
                    using (var p = new Pen(Color.Maroon, 1))
                    { g.FillEllipse(b, fallRect); g.DrawEllipse(p, fallRect); }
                }
                else
                {
                    using (var b = new SolidBrush(Color.Goldenrod))
                    using (var p = new Pen(Color.DarkGoldenrod, 1))
                    { g.FillEllipse(b, fallRect); g.DrawEllipse(p, fallRect); }
                }
            }
        }

        // ===== Local replay UI =====
        private void OpenLocalReplays()
        {
            try
            {
                if (_replayRepo == null) _replayRepo = new LocalReplayRepository();

                using (var dlg = new ReplayPickerForm(_replayRepo, identifier))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        int sessionId = dlg.SelectedSessionId;
                        if (sessionId > 0)
                            PlayLocalSession(sessionId); // async void method below
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to open local replays: " + ex.Message, "Replay",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void PlayLocalSession(int sessionId)
        {
            try
            {
                if (_replayRepo == null) _replayRepo = new LocalReplayRepository();
                var session = _replayRepo.GetSession(sessionId);
                var moves = _replayRepo.LoadSessionMoves(sessionId);

                if (session == null)
                {
                    MessageBox.Show("Session not found.", "Replay",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Load player info for this replay's identifier
                await LoadUserInfoForIdentifierAsync(session.Identifier);

                if (moves == null || moves.Count == 0)
                {
                    MessageBox.Show("No moves in this local session.", "Replay",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ResetBoardUI();

                isReplaying = true;
                isGameOver = false;
                pendingResult = "";
                _replayFinalResult = session.Result ?? "";
                // Important: do not touch gameId during replay

                foreach (var m in moves.OrderBy(m => m.MoveIndex))
                {
                    EnqueueAnim(m.Player, m.Col, m.Row);
                }

                if (!animTimer.Enabled) animTimer.Start();
            }
            catch (Exception ex)
            {
                isReplaying = false;
                MessageBox.Show("Failed to play local replay: " + ex.Message, "Replay",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===== Local replay helpers (recording to local DB) =====
        private void StartLocalRecording(int playerIdentifier, int? serverGameId)
        {
            _replayRepo = new LocalReplayRepository();
            _replaySessionId = _replayRepo.CreateSession(playerIdentifier, serverGameId);
            _moveIndex = 0;
            _localRecordingEnded = false;
        }

        private void RecordMove(int col, int row, int player) // 1=human, 2=computer
        {
            if (_replaySessionId > 0)
                _replayRepo.AddMove(_replaySessionId, _moveIndex++, col, row, player);
        }

        private void FinishLocalRecording(string result) // "PlayerWin"|"ComputerWin"|"Draw"
        {
            if (_replaySessionId > 0 && !_localRecordingEnded)
            {
                try { _replayRepo.EndSession(_replaySessionId, result); } catch { /* ignore */ }
                _localRecordingEnded = true;
                _replaySessionId = 0;
            }
        }

        private static string MapResult(string s)
        {
            switch (s)
            {
                case "PlayerWin":
                case "Win": return "PlayerWin";
                case "ComputerWin":
                case "Lose": return "ComputerWin";
                case "Draw":
                case "Tie": return "Draw";
                default: return "Draw";
            }
        }

        protected override async void OnFormClosed(FormClosedEventArgs e)
        {
            // Replay-only: gameId==0 → do not call server
            if (_startLiveGame && gameId != 0 && !isGameOver && !isReplaying)
            {
                try { await api.EndGameAsync(gameId, "Draw"); } catch { }
                try { FinishLocalRecording("Draw"); } catch { }
            }
            base.OnFormClosed(e);
        }

        // ========== Right-side "Player Info" panel ==========
        private void InitializeUserInfoPanel()
        {
            _infoPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = InfoPanelWidth,
                Padding = new Padding(12),
                BackColor = Color.WhiteSmoke
            };

            _lblTitle = new Label
            {
                Text = "Player Info",
                Font = new Font(Font, FontStyle.Bold),
                AutoSize = true
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _lblNameCaption = new Label { Text = "Name:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            _lblNameValue = new Label { Text = "-", AutoSize = true };
            _lblIdCaption = new Label { Text = "Identifier:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            _lblIdValue = new Label { Text = "-", AutoSize = true };
            _lblCountryCaption = new Label { Text = "Country:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
            _lblCountryValue = new Label { Text = "-", AutoSize = true };

            table.Controls.Add(_lblNameCaption, 0, 0);
            table.Controls.Add(_lblNameValue, 1, 0);
            table.Controls.Add(_lblIdCaption, 0, 1);
            table.Controls.Add(_lblIdValue, 1, 1);
            table.Controls.Add(_lblCountryCaption, 0, 2);
            table.Controls.Add(_lblCountryValue, 1, 2);

            _infoPanel.Controls.Add(table);
            _infoPanel.Controls.Add(_lblTitle);
            _lblTitle.BringToFront();

            this.Controls.Add(_infoPanel); // Docked on the right
        }

        private void UpdateUserInfoPanel(string displayName, int id, string country)
        {
            _lblNameValue.Text = string.IsNullOrWhiteSpace(displayName) ? "(unknown)" : displayName;
            _lblIdValue.Text = id > 0 ? id.ToString() : "-";
            _lblCountryValue.Text = string.IsNullOrWhiteSpace(country) ? "(unknown)" : country;
        }

        // ----- Load user info by Identifier (best effort; falls back to identifier only) -----
        private sealed class UserLite
        {
            public string Username { get; set; } = "";
            public string FirstName { get; set; } = "";
            public int Identifier { get; set; }
            public string Country { get; set; } = "";
        }

        private static readonly HttpClient _userHttp = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7250/"), // keep in sync with your server
            Timeout = TimeSpan.FromSeconds(30)
        };


        private async Task LoadUserInfoForIdentifierAsync(int id)
        {
            try
            {
                // Adjust endpoint to your server route if needed
                var resp = await _userHttp.GetAsync($"api/users/by-identifier/{id}");
                if (!resp.IsSuccessStatusCode)
                {
                    UpdateUserInfoPanel(null, id, null);
                    return;
                }

                var json = await resp.Content.ReadAsStringAsync();
                var user = JsonConvert.DeserializeObject<UserLite>(json);

                if (user == null)
                {
                    UpdateUserInfoPanel(null, id, null);
                    return;
                }

                var name = !string.IsNullOrWhiteSpace(user.Username) ? user.Username : user.FirstName;
                UpdateUserInfoPanel(name, user.Identifier, user.Country);
            }
            catch
            {
                // Offline or endpoint missing → show identifier only
                UpdateUserInfoPanel(null, id, null);
            }
        }

        // ========== Inner picker form ==========
        private sealed class ReplayPickerForm : Form
        {
            private readonly LocalReplayRepository _repo;
            private readonly int _identifier;

            private readonly DataGridView _grid = new DataGridView();
            private readonly BindingSource _bs = new BindingSource();
            private readonly Button _btnRefresh = new Button();
            private readonly Button _btnPlay = new Button();
            private readonly Button _btnCancel = new Button();

            public int SelectedSessionId { get; private set; }

            public ReplayPickerForm(LocalReplayRepository repo, int identifier)
            {
                _repo = repo;
                _identifier = identifier;

                Text = "Local Replays";
                Width = 720;
                Height = 420;
                StartPosition = FormStartPosition.CenterParent;

                _grid.Dock = DockStyle.Fill;
                _grid.ReadOnly = true;
                _grid.AutoGenerateColumns = true;
                _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                _grid.MultiSelect = false;
                _grid.DoubleClick += (s, e) => PlaySelected();

                var panel = new Panel { Dock = DockStyle.Bottom, Height = 44 };
                _btnRefresh.Text = "Refresh"; _btnRefresh.Width = 100; _btnRefresh.Left = 10; _btnRefresh.Top = 8;
                _btnPlay.Text = "Play"; _btnPlay.Width = 100; _btnPlay.Left = 120; _btnPlay.Top = 8;
                _btnCancel.Text = "Cancel"; _btnCancel.Width = 100; _btnCancel.Left = 230; _btnCancel.Top = 8;

                _btnRefresh.Click += (s, e) => LoadData();
                _btnPlay.Click += (s, e) => PlaySelected();
                _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

                panel.Controls.AddRange(new Control[] { _btnRefresh, _btnPlay, _btnCancel });

                Controls.Add(_grid);
                Controls.Add(panel);

                Load += (s, e) => LoadData();
            }

            private void LoadData()
            {
                var list = _repo.ListSessions(_identifier, top: 200)
                                .OrderByDescending(s => s.StartedAt)
                                .ToList();
                _bs.DataSource = list;
                _grid.DataSource = _bs;
                _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            }

            private void PlaySelected()
            {
                if (_grid.CurrentRow != null && _grid.CurrentRow.DataBoundItem is ReplaySessionDto dto)
                {
                    SelectedSessionId = dto.Id;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show("Choose a session first.", "Local Replays",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}

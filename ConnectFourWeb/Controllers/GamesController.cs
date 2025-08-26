using System;
using System.Linq;
using System.Threading.Tasks;
using ConnectFourWeb.Data;
using ConnectFourWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConnectFourWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // /api/games
    public class GamesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public GamesController(ApplicationDbContext db) => _db = db;


        // --- START GAME BY IDENTIFIER ---
        [HttpPost] // POST /api/games
        public async Task<IActionResult> StartGame([FromBody] StartGameDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Identifier == dto.Identifier);
            if (user == null)
                return BadRequest(new { error = $"No user with identifier {dto.Identifier}." });

            var game = new Game
            {
                UserId = user.Id,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Result = ""
            };

            _db.Games.Add(game);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
        }

        // PUT /api/games/{id}/end
        [HttpPut("{id}/end")]
        public async Task<IActionResult> EndGame(int id, [FromBody] EndGameDto dto)
        {
            var game = await _db.Games.FindAsync(id);
            if (game == null) return NotFound();
            game.EndTime = DateTime.UtcNow;
            game.Result = dto.Result ?? "";
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // GET /api/games/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGame(int id)
        {
            var game = await _db.Games
                .Include(g => g.User)
                .Include(g => g.Moves)
                .FirstOrDefaultAsync(g => g.Id == id);

            return game is null ? NotFound() : Ok(game);
        }

        public class PlayerMoveDto { public int Column { get; set; } }
        public class MoveReply { public int Column { get; set; } public int Row { get; set; } public bool IsPlayer { get; set; } }

        // POST /api/games/{id}/player-move
        [HttpPost("{id}/player-move")]
        public async Task<IActionResult> PlayerMove(int id, [FromBody] PlayerMoveDto dto)
        {
            if (dto == null) return BadRequest(new { error = "Missing body" });
            if (dto.Column < 0 || dto.Column > 6) return BadRequest(new { error = "Column must be 0..6" });

            var game = await _db.Games.Include(g => g.Moves).FirstOrDefaultAsync(g => g.Id == id);
            if (game == null) return NotFound(new { error = $"Game {id} not found." });

            // Build board & heights
            var board = new int[6, 7];
            foreach (var m in game.Moves.OrderBy(m => m.TurnNumber))
                board[m.Row, m.Column] = m.IsPlayer ? 1 : 2;

            int[] heights = new int[7];
            for (int c = 0; c < 7; c++)
                for (int r = 0; r < 6; r++)
                    if (board[r, c] != 0) heights[c]++;

            // Player move
            if (heights[dto.Column] >= 6)
                return BadRequest(new { error = "Selected column is full." });

            int playerRow = 5 - heights[dto.Column];
            board[playerRow, dto.Column] = 1;
            heights[dto.Column]++;

            int nextTurn = game.Moves.Any() ? game.Moves.Max(x => x.TurnNumber) + 1 : 1;
            var playerMove = new Move
            {
                GameId = id,
                Column = dto.Column,
                Row = playerRow,
                IsPlayer = true,
                TurnNumber = nextTurn
            };
            _db.Moves.Add(playerMove);

            if (CheckWin(board, 1))
            {
                game.Result = "Win";
                game.EndTime = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(new
                {
                    player = new MoveReply { Column = dto.Column, Row = playerRow, IsPlayer = true },
                    server = (MoveReply?)null,   
                    result = "Win"
                });
            }

            if (IsBoardFull(heights))
            {
                game.Result = "Draw";
                game.EndTime = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(new
                {
                    player = new MoveReply { Column = dto.Column, Row = playerRow, IsPlayer = true },
                    server = (MoveReply?)null,  
                    result = "Draw"
                });
            }

            // Server random legal
            var legalCols = Enumerable.Range(0, 7).Where(c => heights[c] < 6).ToList();
            var rnd = new Random();
            int serverCol = legalCols[rnd.Next(legalCols.Count)];
            int serverRow = 5 - heights[serverCol];
            board[serverRow, serverCol] = 2;
            heights[serverCol]++;

            var serverMove = new Move
            {
                GameId = id,
                Column = serverCol,
                Row = serverRow,
                IsPlayer = false,
                TurnNumber = nextTurn + 1
            };
            _db.Moves.Add(serverMove);

            string result = "InProgress";
            if (CheckWin(board, 2))
            {
                game.Result = "Lose";
                game.EndTime = DateTime.UtcNow;
                result = "Lose";
            }
            else if (IsBoardFull(heights))
            {
                game.Result = "Draw";
                game.EndTime = DateTime.UtcNow;
                result = "Draw";
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                player = new MoveReply { Column = dto.Column, Row = playerRow, IsPlayer = true },
                server = new MoveReply { Column = serverCol, Row = serverRow, IsPlayer = false },
                result
            });
        }

        // ===== helpers =====
        private static bool IsBoardFull(int[] heights)
        {
            for (int c = 0; c < 7; c++) if (heights[c] < 6) return false;
            return true;
        }

        private static bool CheckWin(int[,] board, int p)
        {
            for (int r = 0; r < 6; r++)
                for (int c = 0; c <= 3; c++)
                    if (board[r, c] == p && board[r, c + 1] == p && board[r, c + 2] == p && board[r, c + 3] == p)
                        return true;

            for (int c = 0; c < 7; c++)
                for (int r = 0; r <= 2; r++)
                    if (board[r, c] == p && board[r + 1, c] == p && board[r + 2, c] == p && board[r + 3, c] == p)
                        return true;

            for (int r = 0; r <= 2; r++)
                for (int c = 0; c <= 3; c++)
                    if (board[r, c] == p && board[r + 1, c + 1] == p && board[r + 2, c + 2] == p && board[r + 3, c + 3] == p)
                        return true;

            for (int r = 0; r <= 2; r++)
                for (int c = 3; c < 7; c++)
                    if (board[r, c] == p && board[r + 1, c - 1] == p && board[r + 2, c - 2] == p && board[r + 3, c - 3] == p)
                        return true;

            return false;
        }
    }

    public class StartGameDto { public int Identifier { get; set; } }
    public class EndGameDto { public string Result { get; set; } = ""; }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ConnectFourClient.Api
{
    // Simple DTOs matching server-side API
    public sealed class StartGameRequest
    {
        public int Identifier { get; set; }
    }

    public sealed class MoveResult
    {
        public MoveDto player { get; set; }
        public MoveDto server { get; set; }
        public string result { get; set; } // "PlayerWin" | "ComputerWin" | "Draw" | null
    }

    public sealed class MoveDto
    {
        public int Column { get; set; }
        public int Row { get; set; }      // server should return final landed row
        public int Player { get; set; }   // 1=human, 2=computer
        public int MoveIndex { get; set; }
        public DateTime PlayedAt { get; set; }
    }

    public sealed class GameDto
    {
        public int Id { get; set; }
        public int Identifier { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Result { get; set; } // "PlayerWin" | "ComputerWin" | "Draw" | null
    }

    public class ApiClient : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _base;

        public ApiClient(string baseUrl)
        {
            _base = baseUrl?.TrimEnd('/') ?? "https://localhost:7250";
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public void Dispose() => _http?.Dispose();


        /// <summary>
        /// the function opens a new game by user identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns>gameId</returns>
        public async Task<int> StartGameByIdentifierAsync(int identifier)
        {
            var url = $"{_base}/api/games";
            var body = JsonConvert.SerializeObject(new StartGameRequest { Identifier = identifier });
            var resp = await _http.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json")); // post to server
            resp.EnsureSuccessStatusCode(); // checks if the browser respond with code 2XX
            var s = await resp.Content.ReadAsStringAsync(); //reads the answer
            if (int.TryParse(s, out var directId))  // trying to get the gameid if returnd as a number
                return directId;
            var game = JsonConvert.DeserializeObject<GameDto>(s);   // trying to get the gameid if returnd in object
            return game?.Id ?? 0;
        }


        /// <summary>
        /// Sends the player's move (column index) to the server for the given game,
        /// and returns the server-evaluated result (including the bot's counter-move).
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="column"></param>
        /// <returns>MoveResult</returns>
        public async Task<MoveResult> PlayerMoveAsync(int gameId, int column)
        {
            var url = $"{_base}/api/games/{gameId}/player-move";
            var body = JsonConvert.SerializeObject(new { column });
            var resp = await _http.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));      // Send the HTTP POST with JSON content
            resp.EnsureSuccessStatusCode();
            var s = await resp.Content.ReadAsStringAsync();             // read the response payload as a string
            return JsonConvert.DeserializeObject<MoveResult>(s);        // Deserialize the JSON payload into a MoveResult object and return it
        }



        /// <summary>
        /// Tells the server that the specified game has ended, along with the final result.
        /// </summary>
        public async Task EndGameAsync(int gameId, string result)
        {
            var url = $"{_base}/api/games/{gameId}/end";
            var body = JsonConvert.SerializeObject(new { Result = result ?? "Draw" });

            using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
            using (var resp = await _http.PutAsync(url, content))
            {
                resp.EnsureSuccessStatusCode();
            }
        }




        /// <summary>
        /// Fetches all recorded moves for the given game from the server.
        /// Throws if response status code is not success (non-2xx).
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns>list of MoveDto (empty list if the payload is null).</returns>
        public async Task<IList<MoveDto>> GetGameMovesAsync(int gameId)
        {
            var url = $"{_base}/api/games/{gameId}/moves";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var s = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IList<MoveDto>>(s) ?? new List<MoveDto>();
        }


        /// <summary>
        /// Retrieves a game's details from the server by its ID.
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns>a GameDto</returns>
        public async Task<GameDto> GetGameAsync(int gameId)
        {
            var url = $"{_base}/api/games/{gameId}";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var s = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GameDto>(s);
        }
    }
}

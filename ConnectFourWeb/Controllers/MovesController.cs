using System.Linq;
using System.Threading.Tasks;
using ConnectFourWeb.Data;
using ConnectFourWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConnectFourWeb.Controllers
{
    [ApiController]
    [Route("api/games/{gameId:int}/[controller]")]
    public class MovesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public MovesController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetMoves(int gameId)
        {
            var exists = await _db.Games.AnyAsync(g => g.Id == gameId);
            if (!exists) return NotFound($"Game {gameId} not found.");

            var moves = await _db.Moves
                .Where(m => m.GameId == gameId)
                .OrderBy(m => m.TurnNumber)
                .ToListAsync();

            return Ok(moves);
        }

        [HttpPost]
        public async Task<IActionResult> AddMove(int gameId, [FromBody] MoveDto dto)
        {
            var game = await _db.Games.FindAsync(gameId);
            if (game == null) return BadRequest(new { error = $"Game {gameId} not found." });

            if (dto.Column < 0 || dto.Column > 6) return BadRequest(new { error = "Column must be 0..6" });
            if (dto.Row < 0 || dto.Row > 5) return BadRequest(new { error = "Row must be 0..5" });
            if (dto.TurnNumber <= 0) return BadRequest(new { error = "TurnNumber must be positive" });

            var move = new Move
            {
                GameId = gameId,
                Column = dto.Column,
                Row = dto.Row,
                IsPlayer = dto.IsPlayer,
                TurnNumber = dto.TurnNumber
            };

            _db.Moves.Add(move);
            try { await _db.SaveChangesAsync(); }
            catch (System.Exception ex)
            {
                return Problem(title: "Failed to save move", detail: ex.Message, statusCode: 500);
            }

            return CreatedAtAction(nameof(GetMoves), new { gameId }, new { move.Id, move.Column, move.Row, move.IsPlayer, move.TurnNumber });
        }
    }

    public class MoveDto
    {
        public int Column { get; set; }
        public int Row { get; set; }
        public bool IsPlayer { get; set; }
        public int TurnNumber { get; set; }
    }
}

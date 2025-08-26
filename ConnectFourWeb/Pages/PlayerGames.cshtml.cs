using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConnectFourWeb.Data;
using ConnectFourWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConnectFourWeb.Pages
{
    public class PlayerGamesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public PlayerGamesModel(ApplicationDbContext db) => _db = db;

        // Preserve "login" via query string (?Identifier=...)
        [BindProperty(SupportsGet = true)]
        public int? Identifier { get; set; }

        // Selected player (by Identifier)
        [BindProperty(SupportsGet = true)]
        public int? PlayerIdentifier { get; set; }

        // Dropdown source (distinct players ordered by Username)
        public List<User> Players { get; private set; } = new();

        // Games table; default to empty list to avoid null
        public List<Game> Games { get; private set; } = new();

        public async Task OnGet()
        {
            // Load players (unique by Identifier; ordered by Username asc)
            Players = await _db.Users
                .OrderBy(u => u.Username)
                .ToListAsync();

            // If a player was selected, load that player's games
            if (PlayerIdentifier.HasValue)
            {
                Games = await _db.Games
                    .Include(g => g.User)
                    .Where(g => g.User != null && g.User.Identifier == PlayerIdentifier.Value)
                    .OrderByDescending(g => g.StartTime)
                    .ToListAsync();
            }

            // If no player selected, Games stays as empty list (not null)
        }
    }
}

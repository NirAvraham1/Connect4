using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConnectFourWeb.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConnectFourWeb.Pages
{
    public class PlayersLastGameModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public PlayersLastGameModel(ApplicationDbContext db) => _db = db;

        public IList<PlayerSummary> Players { get; set; } = default!;

        public class PlayerSummary
        {
            public string Username { get; set; } = "";
            public DateTime? LastGameDate { get; set; }
        }

        public async Task OnGetAsync()
        {
            Players = await _db.Users
                .OrderByDescending(u => u.Username.ToLower())
                .Select(u => new PlayerSummary
                {
                    Username = u.Username,
                    LastGameDate = _db.Games
                        .Where(g => g.UserId == u.Id)
                        .OrderByDescending(g => g.StartTime)
                        .Select(g => (DateTime?)g.StartTime)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }
    }
}

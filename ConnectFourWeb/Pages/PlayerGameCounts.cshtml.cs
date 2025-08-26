using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConnectFourWeb.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConnectFourWeb.Pages
{
    public class PlayerGameCountsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public PlayerGameCountsModel(ApplicationDbContext db) => _db = db;

        public IList<PlayerCount> Players { get; set; } = default!;

        public class PlayerCount
        {
            public string Username { get; set; } = "";
            public int GameCount { get; set; }
        }

        public async Task OnGetAsync()
        {
            Players = await _db.Users
                .Select(u => new PlayerCount
                {
                    Username = u.Username,
                    GameCount = _db.Games.Count(g => g.UserId == u.Id)
                   
                })
                .OrderByDescending(pc => pc.GameCount)
                .ToListAsync();
        }
    }
}

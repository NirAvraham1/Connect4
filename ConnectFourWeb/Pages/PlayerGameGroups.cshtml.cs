using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConnectFourWeb.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConnectFourWeb.Pages
{
    public class PlayerGameGroupsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public PlayerGameGroupsModel(ApplicationDbContext db) => _db = db;

        public class PlayerInfo
        {
            public int Id { get; set; }
            public string Username { get; set; } = "";
            public string Country { get; set; } = "";
            public string Phone { get; set; } = "";
            public int Identifier { get; set; }
        }

        public class PlayerGroup
        {
            public int GameCount { get; set; }
            public List<PlayerInfo> Players { get; set; } = new List<PlayerInfo>();
        }

        public List<PlayerGroup> Groups { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var list = await _db.Users
                .Select(u => new
                {
                    User = u,
                    GameCount = _db.Games.Count(g => g.UserId == u.Id)
                })
                .ToListAsync();

            Groups = new List<PlayerGroup>();
            foreach (var count in new[] { 3, 2, 1, 0 })
            {
                var grp = new PlayerGroup { GameCount = count };
                grp.Players = list
                    .Where(x => x.GameCount == count)
                    .Select(x => new PlayerInfo
                    {
                        Id = x.User.Id,
                        Username = x.User.Username,
                        Country = x.User.Country,
                        Phone = x.User.Phone,
                        Identifier = x.User.Identifier
                    })
                    .ToList();
                Groups.Add(grp);
            }
        }
    }
}

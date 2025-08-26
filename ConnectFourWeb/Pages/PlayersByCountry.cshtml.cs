using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConnectFourWeb.Data;
using ConnectFourWeb.Pages;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConnectFourWeb.Pages
{
    public class PlayersByCountryModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public PlayersByCountryModel(ApplicationDbContext db) => _db = db;

        public class PlayerInfo
        {
            public int Id { get; set; }
            public string Username { get; set; } = "";
            public string Phone { get; set; } = "";
            public int Identifier { get; set; }
        }

        public class CountryGroup
        {
            public string Country { get; set; } = "";
            public List<PlayerInfo> Players { get; set; } = new();
        }

        public List<CountryGroup> Groups { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var list = await _db.Users
                .OrderBy(u => u.Country)
                .ThenBy(u => u.Username)
                .ToListAsync();

            Groups = list
                .GroupBy(u => u.Country)
                .Select(g => new CountryGroup
                {
                    Country = g.Key,
                    Players = g.Select(u => new PlayerInfo
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Phone = u.Phone,
                        Identifier = u.Identifier
                    }).ToList()
                })
                .OrderBy(g => g.Country)
                .ToList();
        }
    }
}

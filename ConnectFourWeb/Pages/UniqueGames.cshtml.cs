using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConnectFourWeb.Data;
using ConnectFourWeb.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConnectFourWeb.Pages
{
    public class UniqueGamesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public UniqueGamesModel(ApplicationDbContext db) => _db = db;

        public IList<Game> Games { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var representativeGameIds = await _db.Games
                .Join(_db.Users, g => g.UserId, u => u.Id, (g, u) => new { g.Id, g.StartTime, u.Identifier })
                .GroupBy(x => x.Identifier)
                .Select(grp => grp
                    .OrderByDescending(x => x.StartTime)
                    .ThenByDescending(x => x.Id)
                    .Select(x => x.Id)
                    .First())
                .ToListAsync();

            Games = await _db.Games
                .Where(g => representativeGameIds.Contains(g.Id))
                .Include(g => g.User)
                .OrderByDescending(g => g.StartTime)
                .ToListAsync();
        }
    }
}

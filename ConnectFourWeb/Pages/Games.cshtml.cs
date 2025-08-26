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
    public class GamesModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public GamesModel(ApplicationDbContext db) => _db = db;

        // Logged-in user (?Identifier=...)
        [BindProperty(SupportsGet = true)]
        public int? Identifier { get; set; }

        public List<Game> Games { get; set; } = new();

        public async Task OnGetAsync()
        {
            Games = await _db.Games
                .Include(g => g.User)
                .OrderBy(g => g.Id)
                .ToListAsync();
        }
    }
}

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
    public class PlayersModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public PlayersModel(ApplicationDbContext db) => _db = db;

        // Preserve the "logged-in" user via ?Identifier=...
        [BindProperty(SupportsGet = true)]
        public int? Identifier { get; set; }

        public IList<User> Players { get; set; } = new List<User>();

        public async Task OnGetAsync()
        {
            Players = await _db.Users
                .OrderBy(u => u.Username.ToLower())
                .ToListAsync();
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ConnectFourWeb.Data; 

namespace ConnectFourWeb.Pages
{
    public class GamesDeleteModel : PageModel
    {
        private readonly ApplicationDbContext _db; 

        public GamesDeleteModel(ApplicationDbContext db)
        {
            _db = db;
        }

        // /GamesDelete/{id}
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        // לתצוגה
        public int GameId { get; set; }
        public bool Exists { get; set; }
        public bool DidDelete { get; set; }

        public int? Identifier { get; set; }      // לשימוש ב-purge
        public int? UserIdentifier { get; set; } 
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Result { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(int id)
        {
            GameId = id;

            var game = await _db.Games
                .Include(g => g.User)                  
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                Exists = false;
                return Page();
            }

            Exists = true;
            UserIdentifier = game.User?.Identifier; 
            Identifier = game.User?.Identifier; 
            StartTime = game.StartTime;
            EndTime = game.EndTime;
            Result = game.Result ?? "";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            GameId = id;

            var game = await _db.Games
                .Include(g => g.User)                  
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null)
            {
                Exists = false;
                DidDelete = false;
                return Page();
            }

            UserIdentifier = game.User?.Identifier;
            Identifier = game.User?.Identifier;
            Result = game.Result ?? "";

            _db.Games.Remove(game);
            await _db.SaveChangesAsync();

            DidDelete = true;
            Exists = false;

            return Page();
        }
    }
}

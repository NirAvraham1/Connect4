using System.Threading.Tasks;
using ConnectFourWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConnectFourWeb.Pages.Users
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public DeleteModel(ApplicationDbContext db) => _db = db;

        // Logged-in user Identifier from query string (?Identifier=...)
        [BindProperty(SupportsGet = true)]
        public int? Identifier { get; set; }

        public bool Exists { get; set; }
        public int UserId { get; set; }
        public int? UserIdentifier { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                Exists = false;
                return Page();
            }

            // Allow deleting only yourself
            if (!Identifier.HasValue || user.Identifier != Identifier.Value)
            {
                if (Identifier.HasValue && Identifier.Value > 0)
                    return RedirectToPage("/Players", new { Identifier });
                return RedirectToPage("/Players");
            }

            Exists = true;
            UserId = user.Id;
            UserIdentifier = user.Identifier;
            Username = user.Username;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                if (Identifier.HasValue && Identifier.Value > 0)
                    return RedirectToPage("/Players", new { Identifier });
                return RedirectToPage("/Players");
            }

            // Enforce "delete yourself only"
            if (!Identifier.HasValue || user.Identifier != Identifier.Value)
            {
                if (Identifier.HasValue && Identifier.Value > 0)
                    return RedirectToPage("/Players", new { Identifier });
                return RedirectToPage("/Players");
            }

            // keep the identifier BEFORE deleting (needed for purgeall)
            var deletedIdentifier = user.Identifier;

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            // Redirect to a bridge page that triggers connectfour://purgeall and then returns to /Players
            return RedirectToPage("/Players", new { identifier = deletedIdentifier });
        }

    }
}

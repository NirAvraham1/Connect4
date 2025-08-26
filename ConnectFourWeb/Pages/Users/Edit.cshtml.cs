using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ConnectFourWeb.Data;
using ConnectFourWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectFourWeb.Pages.Users
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public EditModel(ApplicationDbContext db) => _db = db;

        [BindProperty(SupportsGet = true)]
        public int? Identifier { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }
            [Required] public string Username { get; set; } = "";
            [Required] public string Password { get; set; } = "";
            [Required] public string Country { get; set; } = "";
            [RegularExpression(@"^\d{10}$")] public string Phone { get; set; } = "";
            [Range(1, 1000)] public int Identifier { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();

            if (!Identifier.HasValue || u.Identifier != Identifier.Value)
            {
                if (Identifier.HasValue && Identifier.Value > 0)
                    return RedirectToPage("/Players", new { Identifier });
                return RedirectToPage("/Players");
            }

            Input = new InputModel
            {
                Id = u.Id,
                Username = u.Username,
                Password = u.Password,
                Country = u.Country,
                Phone = u.Phone,
                Identifier = u.Identifier
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var u = await _db.Users.FindAsync(Input.Id);
            if (u == null) return NotFound();

            if (!Identifier.HasValue || u.Identifier != Identifier.Value)
            {
                if (Identifier.HasValue && Identifier.Value > 0)
                    return RedirectToPage("/Players", new { Identifier });
                return RedirectToPage("/Players");
            }

            u.Username = Input.Username;
            u.Password = Input.Password;
            u.Country = Input.Country;
            u.Phone = Input.Phone;
            u.Identifier = Input.Identifier;

            await _db.SaveChangesAsync();

            // Keep the login state in the redirect
            if (Identifier.HasValue && Identifier.Value > 0)
                return RedirectToPage("/Players", new { Identifier });

            return RedirectToPage("/Players");
        }
    }
}

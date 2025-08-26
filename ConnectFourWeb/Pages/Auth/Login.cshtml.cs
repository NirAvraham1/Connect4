using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ConnectFourWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConnectFourWeb.Pages.Auth
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public LoginModel(ApplicationDbContext db) => _db = db;

        [BindProperty]
        public LoginInput Input { get; set; } = new LoginInput();

        public void OnGet() {  }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Identifier == Input.Identifier && u.Password == Input.Password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid identifier or password.");
                return Page();
            }

            TempData["Flash"] = "Signed in.";

            return RedirectToPage("/Index", new { identifier = user.Identifier });
        }
    }

    public class LoginInput
    {
        [Required(ErrorMessage = "Identifier is required")]
        [Range(1, 1000, ErrorMessage = "Identifier must be between 1 and 1000")]
        public int Identifier { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Password must be 3-100 characters")]
        public string Password { get; set; } = string.Empty;
    }
}

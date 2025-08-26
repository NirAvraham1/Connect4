using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ConnectFourWeb.Data;
using ConnectFourWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ConnectFourWeb.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public RegisterModel(ApplicationDbContext db) => _db = db;

        [BindProperty]
        public RegisterInput Input { get; set; } = new RegisterInput();

        public List<SelectListItem> CountryOptions { get; private set; } = new();

        private static readonly string[] AllowedCountries = new[]
        { "Israel", "USA", "UK", "France", "Germany", "Italy", "Spain", "Canada", "Australia" };

        public void OnGet() => LoadCountries();

        public async Task<IActionResult> OnPostAsync()
        {
            LoadCountries();

            if (!ModelState.IsValid)
                return Page();

            if (await _db.Users.AnyAsync(u => u.Identifier == Input.Identifier))
            {
                ModelState.AddModelError("Input.Identifier", "Identifier is already in use.");
                return Page();
            }

            if (await _db.Users.AnyAsync(u => u.Username == Input.Username))
            {
                ModelState.AddModelError("Input.Username", "Username is already taken.");
                return Page();
            }

            var user = new User
            {
                Username = Input.Username,
                Password = Input.Password,   
                Identifier = Input.Identifier,
                Phone = Input.Phone,
                Country = Input.Country
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Flash"] = "Registered successfully!";
            return RedirectToPage("/Auth/Login");
        }

        private void LoadCountries()
        {
            CountryOptions = AllowedCountries
                .Select(c => new SelectListItem { Value = c, Text = c })
                .ToList();
        }
    }

    public class RegisterInput
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username must be at most 50 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Password must be 3-100 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Identifier is required")]
        [Range(1, 1000, ErrorMessage = "Identifier must be between 1 and 1000")]
        public int Identifier { get; set; }

        [Required(ErrorMessage = "Phone is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone must be exactly 10 digits")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]
        [StringLength(50, ErrorMessage = "Country must be at most 50 characters")]
        public string Country { get; set; } = string.Empty;
    }
}

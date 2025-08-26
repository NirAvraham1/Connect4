using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectFourWeb.Pages
{
    public class AboutModel : PageModel
    {
        // Keep logged-in state via ?Identifier=...
        [BindProperty(SupportsGet = true)]
        public int? Identifier { get; set; }

        public void OnGet() { }
    }
}

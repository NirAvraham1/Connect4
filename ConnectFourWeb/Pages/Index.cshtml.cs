using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ConnectFourWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        public IndexModel(ILogger<IndexModel> logger) => _logger = logger;

        // Keep the logged-in state via ?Identifier=...
        [BindProperty(SupportsGet = true)]
        public int? Identifier { get; set; }

        public void OnGet()
        {
        }
    }
}

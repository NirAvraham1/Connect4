using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectFourWeb.Pages.Replay
{
    public class IndexModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int? Identifier { get; set; }

        public void OnGet()
        {
           
        }
    }
}

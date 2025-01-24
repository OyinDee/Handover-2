using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Handover_2.Pages
{
    public class NotFoundModel : PageModel
    {
        public void OnGet()
        {
            Response.StatusCode = 404;
        }
    }
}

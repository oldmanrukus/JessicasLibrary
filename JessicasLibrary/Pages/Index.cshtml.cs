using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace JessicasLibrary.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // If logged in, redirect to the book shelf
                return RedirectToPage("/Books/Shelf");
            }
            return Page();
        }
    }
}

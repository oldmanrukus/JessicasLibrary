using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JessicasLibrary.Services;

namespace JessicasLibrary.Pages.Admin.Books
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly FirebaseService _firebase;
        public EditModel(FirebaseService firebase) => _firebase = firebase;

        [BindProperty] public string BookId { get; set; } = "";
        [BindProperty] public string Description { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(string bookId)
        {
            BookId = bookId;
            var meta = await _firebase.GetBookAsync(bookId);
            if (meta == null) return NotFound();

            Description = meta.Description;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var meta = await _firebase.GetBookAsync(BookId);
            if (meta == null) return NotFound();

            meta.Description = Description;
            await _firebase.SaveBookMetadataAsync(BookId, meta);
            return RedirectToPage("./Index");
        }
    }
}

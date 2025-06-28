using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using JessicasLibrary.Models;
using JessicasLibrary.Services;

namespace JessicasLibrary.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class BookAccessModel : PageModel
    {
        private readonly FirebaseService _firebase;
        private readonly UserManager<IdentityUser> _userManager;

        public BookAccessModel(FirebaseService firebase,
                               UserManager<IdentityUser> userManager)
        {
            _firebase = firebase;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public string BookId { get; set; } = "";

        public BookMetadata? Metadata { get; set; }

        public List<IdentityUser> AllUsers { get; set; } = new();

        [BindProperty]
        public List<string> SelectedUserIds { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string bookId)
        {
            BookId = bookId;
            Metadata = await _firebase.GetBookAsync(bookId);
            if (Metadata == null)
            {
                return NotFound();
            }

            // load all non-owner users
            AllUsers = await _userManager.Users
                             .Where(u => u.Id != Metadata.OwnerUserId)
                             .ToListAsync();

            // prepopulate selected list
            SelectedUserIds = Metadata.AllowedUserIds;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // reload metadata
            Metadata = await _firebase.GetBookAsync(BookId);
            if (Metadata == null)
            {
                return NotFound();
            }

            // update allowed list
            Metadata.AllowedUserIds = SelectedUserIds ?? new List<string>();

            // save back to Firebase
            await _firebase.SaveBookMetadataAsync(BookId, Metadata);

            return RedirectToPage("/Admin/Books/Index");
        }
    }
}

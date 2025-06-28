using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JessicasLibrary.Models;
using JessicasLibrary.Services;

namespace JessicasLibrary.Pages.Books
{
    [Authorize]
    public class ShelfModel : PageModel
    {
        private readonly FirebaseService _firebase;

        public ShelfModel(FirebaseService firebase) => _firebase = firebase;

        public Dictionary<string, BookMetadata> Books { get; private set; } = new();

        public async Task OnGetAsync() => Books = await _firebase.GetBooksAsync();
    }
}

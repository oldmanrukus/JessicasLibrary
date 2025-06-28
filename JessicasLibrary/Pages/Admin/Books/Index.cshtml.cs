using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenXmlPowerTools;
using JessicasLibrary.Models;
using JessicasLibrary.Services;

namespace JessicasLibrary.Pages.Admin.Books
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly FirebaseService _firebase;

        public IndexModel(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        // Bound upload fields
        [BindProperty] public string Title { get; set; } = "";
        [BindProperty] public string Genre { get; set; } = "";
        [BindProperty] public string Description { get; set; } = "";
        [BindProperty] public IFormFile DocFile { get; set; } = default!;
        [BindProperty] public IFormFile CoverFile { get; set; } = default!;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        // For listing
        public Dictionary<string, BookMetadata> Books { get; private set; } = new();

        public async Task OnGetAsync()
        {
            Books = await _firebase.GetBooksAsync();
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (string.IsNullOrWhiteSpace(Title)
             || string.IsNullOrWhiteSpace(Genre)
             || DocFile == null
             || CoverFile == null)
            {
                ErrorMessage = "All fields are required.";
                await OnGetAsync();
                return Page();
            }

            var bookId = Guid.NewGuid().ToString("N");
            try
            {
                // Upload .docx
                string docUrl;
                using var ds = DocFile.OpenReadStream();
                docUrl = await _firebase.UploadBookAsync(bookId, DocFile.FileName, ds);

                // Upload cover
                string coverUrl;
                using var cs = CoverFile.OpenReadStream();
                coverUrl = await _firebase.UploadCoverAsync(bookId, CoverFile.FileName, cs);

                // Download doc to memory for page counting
                byte[] bytes;
                using var http = new HttpClient();
                bytes = await http.GetByteArrayAsync(docUrl);
                using var ms = new MemoryStream();
                await ms.WriteAsync(bytes, 0, bytes.Length);
                ms.Position = 0;

                // Convert to HTML
                using var wdoc = WordprocessingDocument.Open(ms, true);
                var html = HtmlConverter.ConvertToHtml(wdoc, new HtmlConverterSettings
                {
                    PageTitle = Title,
                    FabricateCssClasses = true,
                    CssClassPrefix = "doc-",
                    RestrictToSupportedLanguages = false,
                    RestrictToSupportedNumberingFormats = false,
                    ImageHandler = _ => null
                });

                // Split on </p> for paragraphs
                var body = html.Descendants(Xhtml.body).FirstOrDefault();
                var h = body != null
                    ? body.ToString()
                    : html.ToString();
                var paras = System.Text.RegularExpressions.Regex
                    .Split(h, @"(?<=</p>)")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                int pageCount = (int)Math.Ceiling(paras.Count / 5.0);

                // Save metadata
                var meta = new BookMetadata
                {
                    Title = Title,
                    Genre = Genre,
                    Description = Description,
                    FileUrl = docUrl,
                    CoverUrl = coverUrl,
                    OwnerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    PageCount = pageCount
                };
                await _firebase.SaveBookMetadataAsync(bookId, meta);

                SuccessMessage = "Book uploaded successfully!";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Upload failed: " + ex.Message;
            }

            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string bookId)
        {
            await _firebase.DeleteBookAsync(bookId);
            SuccessMessage = "Book deleted.";
            await OnGetAsync();
            return Page();
        }
    }
}

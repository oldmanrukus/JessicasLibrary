using System;
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

namespace JessicasLibrary.Pages.Books
{
    [Authorize(Roles = "Admin")]
    public class UploadModel : PageModel
    {
        private readonly FirebaseService _firebase;

        public UploadModel(FirebaseService firebase)
            => _firebase = firebase;

        [BindProperty]
        public string Title { get; set; } = "";

        [BindProperty]
        public string Genre { get; set; } = "";

        [BindProperty]
        public IFormFile DocFile { get; set; } = default!;

        [BindProperty]
        public IFormFile CoverFile { get; set; } = default!;

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Title)
                || string.IsNullOrWhiteSpace(Genre)
                || DocFile == null
                || CoverFile == null)
            {
                ErrorMessage = "All fields are required.";
                return Page();
            }

            var bookId = Guid.NewGuid().ToString("N");

            try
            {
                // 1) Upload the .docx
                string docUrl;
                using (var stream = DocFile.OpenReadStream())
                {
                    docUrl = await _firebase.UploadBookAsync(bookId, DocFile.FileName, stream);
                }

                // 2) Upload the cover image
                string coverUrl;
                using (var stream = CoverFile.OpenReadStream())
                {
                    coverUrl = await _firebase.UploadCoverAsync(bookId, CoverFile.FileName, stream);
                }

                // 3) Download back the .docx for page-count calculation
                byte[] bytes;
                using (var client = new HttpClient())
                {
                    bytes = await client.GetByteArrayAsync(docUrl);
                }

                // Expandable MemoryStream
                var ms = new MemoryStream();
                await ms.WriteAsync(bytes, 0, bytes.Length);
                ms.Position = 0;

                // Convert to HTML to count pages
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

                // Split into paragraphs
                var body = html.Descendants(Xhtml.body).FirstOrDefault();
                var paras = body != null
                    ? body.Elements().Select(e => e.ToString()).ToList()
                    : new List<string> { html.ToString() };

                // Group into pages of 5 paragraphs each
                const int parasPerPage = 5;
                var pages = paras
                   .Select((p, i) => new { p, i })
                   .GroupBy(x => x.i / parasPerPage)
                   .ToList();

                int pageCount = pages.Count;

                // 4) Save metadata
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var meta = new BookMetadata
                {
                    Title = Title,
                    Genre = Genre,
                    FileUrl = docUrl,
                    CoverUrl = coverUrl,
                    OwnerUserId = userId,
                    PageCount = pageCount
                };
                await _firebase.SaveBookMetadataAsync(bookId, meta);

                return RedirectToPage("/Books/Shelf");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Upload failed: " + ex.Message;
                return Page();
            }
        }
    }
}

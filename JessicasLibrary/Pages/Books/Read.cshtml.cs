using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenXmlPowerTools;
using JessicasLibrary.Models;
using JessicasLibrary.Services;
using System.Xml.Linq;

namespace JessicasLibrary.Pages.Books
{
    [Authorize]
    public class ReadModel : PageModel
    {
        private readonly FirebaseService _firebaseService;
        private readonly AzureSpeechService _speechService;

        public ReadModel(FirebaseService firebaseService,
                         AzureSpeechService speechService)
        {
            _firebaseService = firebaseService;
            _speechService = speechService;
        }

        [BindProperty(SupportsGet = true)]
        public string? BookId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public string? BookTitle { get; set; }
        public List<string> Pages { get; private set; } = new();
        public int TotalPages => Pages.Count;
        public bool IsAudioAvailable => Pages.Count > 0;

        public async Task<IActionResult> OnGetAsync(string? bookId, int pageNumber = 1)
        {
            if (string.IsNullOrEmpty(bookId))
                return NotFound("Book ID not specified.");

            BookId = bookId;
            PageNumber = Math.Max(1, pageNumber);

            // 1) Fetch metadata
            var meta = await _firebaseService.GetBookAsync(bookId);
            if (meta == null) return NotFound("Book not found.");
            BookTitle = meta.Title;

            // 2) Authorization: Admin, Owner, Public, or AllowedUserIds
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
            var isAdmin = User.IsInRole("Admin");
            var isOwner = meta.OwnerUserId == userId;
            var isAllowed = meta.IsPublic || meta.AllowedUserIds.Contains(userId);

            if (!isAdmin && !isOwner && !isAllowed)
                return Forbid();

            // 3) Download & convert DOCX → HTML
            byte[] docBytes;
            using (var http = new System.Net.Http.HttpClient())
                docBytes = await http.GetByteArrayAsync(meta.FileUrl);

            using var ms = new MemoryStream();
            await ms.WriteAsync(docBytes, 0, docBytes.Length);
            ms.Position = 0;

            using var wdoc = WordprocessingDocument.Open(ms, true);
            var settings = new HtmlConverterSettings
            {
                PageTitle = meta.Title,
                FabricateCssClasses = true,
                CssClassPrefix = "doc-",
                RestrictToSupportedLanguages = false,
                RestrictToSupportedNumberingFormats = false,
                ImageHandler = info => null
            };
            var html = HtmlConverter.ConvertToHtml(wdoc, settings);

            // 4) Extract <body> and split on </p> so each paragraph is its own string
            var bodyElem = html.Descendants(Xhtml.body).FirstOrDefault();
            var bodyHtml = bodyElem != null
                ? bodyElem.ToString(SaveOptions.DisableFormatting)
                : html.ToString(SaveOptions.DisableFormatting);

            var paras = Regex
                .Split(bodyHtml, @"(?<=</p>)")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            // 5) Chunk into pages of N paragraphs each
            const int ParasPerPage = 5;
            Pages = paras
                .Select((p, i) => new { p, i })
                .GroupBy(x => x.i / ParasPerPage)
                .Select(g => string.Concat(g.Select(x => x.p)))
                .ToList();

            if (PageNumber > Pages.Count)
                PageNumber = Pages.Count;

            return Page();
        }

        public async Task<IActionResult> OnGetAudioAsync(string bookId, int pageNumber = 1)
        {
            // Re-run OnGetAsync to populate Pages
            await OnGetAsync(bookId, pageNumber);
            if (!IsAudioAvailable)
                return NotFound();

            // Plain-text from current page only
            var text = Regex.Replace(Pages[PageNumber - 1], "<[^>]+>", " ");
            var audio = await _speechService.SynthesizeSpeechAsync(text);
            return File(audio, "audio/wav");
        }
    }
}

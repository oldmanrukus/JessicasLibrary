using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Firebase.Storage;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using JessicasLibrary.Models;

namespace JessicasLibrary.Services
{
    /// <summary>
    /// Handles uploads to Firebase Storage and metadata CRUD in Realtime DB.
    /// Relies on a single FirebaseOptions POCO defined in FirebaseOptions.cs.
    /// </summary>
    public class FirebaseService
    {
        private readonly FirebaseOptions _opts;
        private readonly HttpClient _http;

        public FirebaseService(
            IOptions<FirebaseOptions> optsAccessor,
            HttpClient httpClient)
        {
            _opts = optsAccessor.Value;
            _http = httpClient;
        }

        /// <summary> Uploads a .docx and returns its public URL. </summary>
        public async Task<string> UploadBookAsync(string bookId, string fileName, Stream fileStream)
        {
            var storage = new FirebaseStorage(
                _opts.StorageBucket,
                new FirebaseStorageOptions { ThrowOnCancel = true }
            );

            return await storage
                .Child("books")
                .Child(bookId + Path.GetExtension(fileName))
                .PutAsync(fileStream);
        }

        /// <summary> Uploads a .png cover image and returns its public URL. </summary>
        public async Task<string> UploadCoverAsync(string bookId, string fileName, Stream fileStream)
        {
            var storage = new FirebaseStorage(
                _opts.StorageBucket,
                new FirebaseStorageOptions { ThrowOnCancel = true }
            );

            return await storage
                .Child("covers")
                .Child(bookId + Path.GetExtension(fileName))
                .PutAsync(fileStream);
        }

        /// <summary> Saves or updates the book metadata under /books/{bookId}. </summary>
        public async Task SaveBookMetadataAsync(string bookId, BookMetadata metadata)
        {
            string baseUrl = _opts.BasePath.TrimEnd('/');
            string url = $"{baseUrl}/books/{bookId}.json?auth={_opts.AuthSecret}";

            string json = JsonConvert.SerializeObject(metadata);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _http.PutAsync(url, content);
            resp.EnsureSuccessStatusCode();
        }

        /// <summary> Retrieves all books metadata as a dictionary. </summary>
        public async Task<Dictionary<string, BookMetadata>> GetBooksAsync()
        {
            string baseUrl = _opts.BasePath.TrimEnd('/');
            string url = $"{baseUrl}/books.json?auth={_opts.AuthSecret}";

            string json = await _http.GetStringAsync(url);
            if (string.IsNullOrEmpty(json) || json == "null")
                return new Dictionary<string, BookMetadata>();

            return JsonConvert
                .DeserializeObject<Dictionary<string, BookMetadata>>(json)
                ?? new Dictionary<string, BookMetadata>();
        }

        /// <summary> Retrieves a single book's metadata by ID. </summary>
        public async Task<BookMetadata?> GetBookAsync(string bookId)
        {
            string baseUrl = _opts.BasePath.TrimEnd('/');
            string url = $"{baseUrl}/books/{bookId}.json?auth={_opts.AuthSecret}";

            string json = await _http.GetStringAsync(url);
            if (string.IsNullOrEmpty(json) || json == "null")
                return null;

            return JsonConvert.DeserializeObject<BookMetadata>(json);
        }

        /// <summary> Deletes a book’s metadata (and can be extended for storage). </summary>
        public async Task DeleteBookAsync(string bookId)
        {
            // Remove metadata
            string baseUrl = _opts.BasePath.TrimEnd('/');
            string url = $"{baseUrl}/books/{bookId}.json?auth={_opts.AuthSecret}";
            var resp = await _http.DeleteAsync(url);
            resp.EnsureSuccessStatusCode();

            // Optionally, delete from storage via REST API or Admin SDK:
            // - books/{bookId}.docx
            // - covers/{bookId}.png
        }
    }
}

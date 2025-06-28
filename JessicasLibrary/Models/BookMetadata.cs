namespace JessicasLibrary.Models
{
    public class BookMetadata
    {
        public string Title { get; set; } = "";
        public string Genre { get; set; } = "";
        public string FileUrl { get; set; } = "";
        public string CoverUrl { get; set; } = "";
        public string OwnerUserId { get; set; } = "";
        public string Description { get; set; } = "";
        public int PageCount { get; set; }

        // ─── NEW ───
        /// <summary>If true, every signed-in user may read</summary>
        public bool IsPublic { get; set; } = false;

        /// <summary>
        /// If IsPublic==false, only these user-IDs may read
        /// </summary>
        public List<string> AllowedUserIds { get; set; } = new();
    }
}

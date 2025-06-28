namespace JessicasLibrary.Services
{
    /// <summary>
    /// Binds to the "Firebase" section of appsettings.json.
    /// </summary>
    public class FirebaseOptions
    {
        /// <summary>
        /// e.g. "https://your-db.firebaseio.com/"
        /// </summary>
        public string BasePath { get; set; } = "";

        /// <summary>
        /// Your Firebase Realtime Database secret or token.
        /// </summary>
        public string AuthSecret { get; set; } = "";

        /// <summary>
        /// Your Storage bucket name (e.g. "your-app.appspot.com").
        /// </summary>
        public string StorageBucket { get; set; } = "";
    }
}

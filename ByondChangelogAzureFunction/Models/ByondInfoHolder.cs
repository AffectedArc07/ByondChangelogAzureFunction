namespace ByondChangelogAzureFunction.Models
{
    internal class ByondInfoHolder
    {
        /// <summary>
        /// Outer type name, be it "Fixes" or "Features" or whatever else.
        /// </summary>
        public string OuterTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Outer type link for the resolved things on the BYOND forums.
        /// </summary>
        public string OuterTypeLink { get; set; } = string.Empty;

        /// <summary>
        /// The list of application entries for this CL entry.
        /// </summary>
        public List<ApplicationChangelogHolder> ApplicationEntries { get; } = new();

        /// <summary>
        /// The application holder class. Used for entries such as Dream Maker, Dream Seeker, etc.
        /// </summary>
        internal class ApplicationChangelogHolder {
            /// <summary>
            /// The name of the application. "Dream Seeker" or "Dream Maker" or etc.
            /// </summary>
            public string ApplicationName { get; set; } = string.Empty;

            /// <summary>
            /// The CL entries for this application.
            /// </summary>
            public List<string> Entries { get; } = new();
        }
    }
}

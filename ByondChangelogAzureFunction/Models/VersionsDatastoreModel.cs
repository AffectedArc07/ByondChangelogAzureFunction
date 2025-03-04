using System.Text.Json.Serialization;

namespace ByondChangelogAzureFunction.Models
{
    /// <summary>
    /// Represents the schema for the bv_data/versions.json file.
    /// </summary>
    class VersionsDatastoreModel
    {
        /// <summary>
        /// The stored stable version.
        /// </summary>
        [JsonPropertyName("stable")]
        public string StableVersion { get; set; } = string.Empty;

        /// <summary>
        /// The stored beta version.
        /// </summary>
        [JsonPropertyName("beta")]
        public string BetaVersion { get; set; } = string.Empty;
    }
}

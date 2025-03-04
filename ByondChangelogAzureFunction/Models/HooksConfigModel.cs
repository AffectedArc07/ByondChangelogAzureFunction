using System.Text.Json.Serialization;

namespace ByondChangelogAzureFunction.Models
{
    /// <summary>
    /// Represents the schema for the bv_data/hooks.toml file.
    /// </summary>
    public class HooksConfigModel
    {
        /// <summary>
        /// List of webhook URLs.
        /// </summary>
        [JsonPropertyName("hooks")] // Yes I know this is toml but it uses the json attributes.
        public List<string> HookUrls { get; set; } = new();
    }
}

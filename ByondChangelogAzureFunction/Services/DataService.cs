using ByondChangelogAzureFunction.Models;
using System.Text.Json;
using Tomlyn;

namespace ByondChangelogAzureFunction.Services
{
    /// <summary>
    /// Implementation of <see cref="IDataService"/>.
    /// </summary>
    public class DataService : IDataService {
        /// <summary>
        /// The data dir. Used by <see cref="DatastoreFile"/> and <see cref="HooksFile"/>. Do not ever change.
        /// </summary>
        private const string DataDir = "bv_data";

        /// <summary>
        /// Used to store the local cache of the latest BYOND version. Do not ever change.
        /// </summary>
        private const string DatastoreFile = $"{DataDir}/versions.json";

        /// <summary>
        /// Used to store the list of webhooks to send updates to. Do not ever change.
        /// </summary>
        private const string HooksFile = $"{DataDir}/hooks.toml";

        /// <summary>
        /// Creates a new <see cref="DataService"/> whilst also bootstrapping the data dir.
        /// This is automatically invoked by the functions runtime, do not manually call.
        /// </summary>
        public DataService() {
            // Create our data dir if it doesnt exist
            if (!Directory.Exists(DataDir)) {
                Directory.CreateDirectory(DataDir);
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<ByondReleaseChannel, string>> GetByondVersions() {
            if (!File.Exists(DatastoreFile)) {
                // Create a blank datastore
                VersionsDatastoreModel blank_store = new();
                string json_data = JsonSerializer.Serialize(blank_store);
                await File.WriteAllTextAsync(DatastoreFile, json_data);
            }

            // Read the data from the file
            string file_data = await File.ReadAllTextAsync(DatastoreFile);

            if (string.IsNullOrWhiteSpace(file_data)) {
                throw new Exception($"{DatastoreFile} is blank - delete and recreate!");
            }

            // Load it to the model
            VersionsDatastoreModel? data_model = JsonSerializer.Deserialize<VersionsDatastoreModel>(file_data);

            if (data_model == null) {
                throw new Exception($"{DatastoreFile} is malformed - delete and recreate!");
            }

            // Read our data and assign the right versions
            Dictionary<ByondReleaseChannel, string> version_dict = new();

            if (!string.IsNullOrWhiteSpace(data_model.StableVersion)) {
                version_dict[ByondReleaseChannel.Stable] = data_model.StableVersion;
            }

            if (!string.IsNullOrWhiteSpace(data_model.BetaVersion)) {
                version_dict[ByondReleaseChannel.Beta] = data_model.BetaVersion;
            }

            // Then send it back
            return version_dict;
        }



        /// <inheritdoc/>
        public async Task<List<string>> GetWebhooks() {
            if (!File.Exists(HooksFile)) {
                // Create a blank hooks file
                HooksConfigModel blank_config = new();
                // Add a blank URL to format the file properly
                blank_config.HookUrls.Add("https://example.com");
                string toml_data = Toml.FromModel(blank_config);
                await File.WriteAllTextAsync(HooksFile, toml_data);
            }

            // Read the data from the file
            string file_data = await File.ReadAllTextAsync(HooksFile);

            if (string.IsNullOrWhiteSpace(file_data)) {
                throw new Exception($"{HooksFile} is blank - delete and recreate!");
            }

            // Load it to the model
            HooksConfigModel? data_model = Toml.ToModel<HooksConfigModel>(file_data);

            if (data_model == null) {
                throw new Exception($"{HooksFile} is malformed - delete and recreate!");
            }

            return data_model.HookUrls;
        }



        /// <inheritdoc/>
        public async Task WriteByondVersions(Dictionary<ByondReleaseChannel, string> versions) {
            // Load our existing ones
            Dictionary<ByondReleaseChannel, string> existing_versions = await GetByondVersions();

            // Replace them with the applicable new ones
            foreach(ByondReleaseChannel channel in versions.Keys) {
                existing_versions[channel] = versions[channel];
            }

            // Serialise to model
            VersionsDatastoreModel model = new();

            if (existing_versions.ContainsKey(ByondReleaseChannel.Stable)) {
                model.StableVersion = existing_versions[ByondReleaseChannel.Stable];
            }

            if (existing_versions.ContainsKey(ByondReleaseChannel.Beta)) {
                model.BetaVersion = existing_versions[ByondReleaseChannel.Beta];
            }

            // Serialise to text
            string json_text = JsonSerializer.Serialize(model);

            // And write out
            await File.WriteAllTextAsync(DatastoreFile, json_text);
        }
    }
}

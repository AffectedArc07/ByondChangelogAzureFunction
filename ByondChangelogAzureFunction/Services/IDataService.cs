using ByondChangelogAzureFunction.Models;

namespace ByondChangelogAzureFunction.Services
{
    /// <summary>
    /// Service to handle file IO.
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// Gets all the webhooks to send data to
        /// </summary>
        /// <returns>A <see cref="List"/> of <see cref="string"/> webhook URLs.</returns>
        public Task<List<string>> GetWebhooks();

        /// <summary>
        /// Gets the list of BYOND versions we have on file.
        /// </summary>
        /// <returns>A <see cref="Dictionary"/>, keyed with the <see cref="ByondReleaseChannel"/> and a value of the <see cref="string"/> BYOND version.</returns>
        public Task<Dictionary<ByondReleaseChannel, string>> GetByondVersions();

        /// <summary>
        /// Writes the list of BYOND versions back to the datastore.
        /// If a version is not supplied, the existing one on file is preserved.
        /// </summary>
        /// <param name="versions">The <see cref="Dictionary{ByondReleaseChannel, string}"/> of BYOND versions to write.</param>
        public Task WriteByondVersions(Dictionary<ByondReleaseChannel, string> versions);
    }
}

namespace ByondChangelogAzureFunction.Models
{
    /// <summary>
    /// Represents a BYOND release channel.
    /// </summary>
    public enum ByondReleaseChannel
    {
        Stable,
        Beta
    }

    /// <summary>
    /// Extension to <see cref="ByondReleaseChannel"/> to support pretty printing.
    /// </summary>
    public static class ByondVersionEnumExtensions {
        public static string ToFormattedName(this ByondReleaseChannel channel) {
            // Switch what we have
            switch(channel) {
                case ByondReleaseChannel.Stable:
                    return "Stable";
                case ByondReleaseChannel.Beta:
                    return "Beta";
                default:
                    // This should never happen
                    throw new Exception($"Invalid ByondReleaseChannel value - {channel}");
            }
        }
    }
}

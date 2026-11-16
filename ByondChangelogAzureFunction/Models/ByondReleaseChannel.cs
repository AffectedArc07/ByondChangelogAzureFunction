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
            return channel switch {
                ByondReleaseChannel.Stable => "Stable",
                ByondReleaseChannel.Beta => "Beta",
                _ => throw new Exception($"Invalid ByondReleaseChannel value - {channel}"),// This should never happen
            };
        }
    }
}

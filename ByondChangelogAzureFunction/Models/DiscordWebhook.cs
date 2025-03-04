using System.Text.Json.Serialization;

namespace ByondChangelogAzureFunction.Models {
    /// <summary>
    /// Represents a discord webhook.
    /// See https://discord.com/developers/docs/resources/webhook#execute-webhook
    /// </summary>
    internal class DiscordWebhook {
        /// <summary>
        /// The user for the webhook.
        /// </summary>
        [JsonPropertyName("username")]
        public string Username { get; } = "BYOND Changelog";

        /// <summary>
        /// The avatar to use for the webhook.
        /// </summary>

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; } = "http://mocha.affectedarc07.co.uk/byond.webp";

        /// <summary>
        /// The embeds on the webhook.
        /// </summary>
        [JsonPropertyName("embeds")]
        public List<EmbedHolder> Embeds { get; } = new();

        internal class EmbedHolder {
            /// <summary>
            /// Title for the embed.
            /// </summary>
            [JsonPropertyName("title")]
            public string Title { get; }

            /// <summary>
            /// The URL for the BYOND download, in the embed.
            /// </summary>
            [JsonPropertyName("url")]
            public string Url { get; } = "https://secure.byond.com/download/";

            /// <summary>
            /// The timestamp we sent the embed.
            /// </summary>
            [JsonPropertyName("timestamp")]
            public string Timestamp { get; } = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"); // no idea if I need to surround each piece with '' but I am doing it anyway


            /// <summary>
            /// The footer for the embed.
            /// </summary>
            [JsonPropertyName("footer")]
            public FooterHolder Footer { get; } = new();

            /// <summary>
            /// The footer for the webhook.
            /// </summary>
            [JsonPropertyName("fields")]
            public List<EmbedField> Fields { get; } = new();


            /// <summary>
            /// Represents the footer.
            /// </summary>
            internal class FooterHolder {
                /// <summary>
                /// Text for the webhook footer.
                /// </summary>
                [JsonPropertyName("text")]
                public string FooterText { get; } = "https://github.com/AffectedArc07/ByondChangelogAzureFunction";
            }



            /// <summary>
            /// Represents a field on a discord webhook.
            /// </summary>
            internal class EmbedField {
                /// <summary>
                /// The name of the field.
                /// </summary>
                [JsonPropertyName("name")]
                public string FieldName { get; }

                /// <summary>
                /// The content of the field.
                /// </summary>
                [JsonPropertyName("value")]
                public string FieldValue { get; }

                /// <summary>
                /// Creates a new <see cref="EmbedField"/>.
                /// </summary>
                /// <param name="fieldName">The <see cref="string"/> title for this embed field.</param>
                /// <param name="fieldValue">The <see cref="string"/> content for this embed field.</param>
                public EmbedField(string fieldName, string fieldValue) {
                    FieldName = fieldName;
                    FieldValue = fieldValue;
                }
            }



            /// <summary>
            /// Creates a new <see cref="EmbedHolder"/> model.
            /// </summary>
            /// <param name="versionTag">The <see cref="string"/> version tag - IE: 515.1656</param>
            /// <param name="releaseChannel">The <see cref="ByondReleaseChannel"/> for this changelog.</param>
            /// <param name="byondInfo">A <see cref="List"/> of <see cref="ByondInfoHolder"/>s to be put into the CL.</param>
            public EmbedHolder(string versionTag, ByondReleaseChannel releaseChannel, List<ByondInfoHolder> byondInfo) {
                // Set title
                Title = $"BYOND version {versionTag} ({releaseChannel.ToFormattedName()})";

                // Loop through each one
                foreach(ByondInfoHolder bih in byondInfo) {
                    // First the field for the section name
                    string field_value = $"**__[{bih.OuterTypeName}]({bih.OuterTypeLink})__**";
                    Fields.Add(new("\u200b", field_value));

                    
                    // Handle the individual entries
                    foreach(ByondInfoHolder.ApplicationChangelogHolder ach in bih.ApplicationEntries) {
                        string current_title = ach.ApplicationName;
                        string current_content = string.Empty;

                        // Handle each line in each application
                        foreach (string entry in ach.Entries) {
                            // Sanity check for length
                            if ((current_content.Length + entry.Length) > 1000) {
                                // Add a field with what we have and make a continuation
                                Fields.Add(new(current_title, current_content));
                                current_title = $"{ach.ApplicationName} (Continued)";
                                current_content = string.Empty;
                            }

                            current_content += $"\u25CF {entry}\n";
                        }
                        
                        // And add the field - this will break on big updates as embeds have a cap of 25 fields
                        Fields.Add(new(current_title, current_content));
                    }
                }
            }
        }


        /// <summary>
        /// Creates a new <see cref="DiscordWebhook"/> model.
        /// </summary>
        /// <param name="versionTag">The <see cref="string"/> BYOND version tag: IE - 516.1656</param>
        /// <param name="releaseChannel">The <see cref="string"/> BYOND release channel. 'Beta' or 'Stable' only please.</param>
        public DiscordWebhook(string versionTag, ByondReleaseChannel releaseChannel, List<ByondInfoHolder> byondInfo) {
            // Just pass the info as an embed
            Embeds.Add(new EmbedHolder(versionTag, releaseChannel, byondInfo));
        }
    }
}

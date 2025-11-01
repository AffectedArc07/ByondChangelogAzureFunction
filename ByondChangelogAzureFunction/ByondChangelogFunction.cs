using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using ByondChangelogAzureFunction.Models;
using ByondChangelogAzureFunction.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace ByondChangelogAzureFunction {
    public class ByondChangelogFunction {
        /// <summary>
        /// The <see cref="ILogger"/> for the <see cref="ByondChangelogFunction"/>.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The <see cref="IDataService"/> for the <see cref="ByondChangelogFunction"/>.
        /// </summary>
        private readonly IDataService _dataService;

        /// <summary>
        /// The <see cref="HttpClient"/> for the <see cref="ByondChangelogFunction"/>.
        /// </summary>
        private readonly HttpClient _apiClient;

        /// <summary>
        /// Creates the <see cref="ByondChangelogFunction"/> handler.
        /// Automatically invoked by the functions runtime - do not manually instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> for the <see cref="ByondChangelogFunction"/>.</param>
        /// <param name="dataService">The <see cref="IDataService"/> for the <see cref="ByondChangelogFunction"/>.</param>
        public ByondChangelogFunction(ILogger<ByondChangelogFunction> logger, IDataService dataService) {
            // Assign what we pass through.
            _logger = logger;
            _dataService = dataService;

            // Create our HTTP client options.
            // If we dont set this, we can run out of TCP states and requests dont go through.
            // Dont ask me how I know this.
            HttpClientHandler options = new();
            options.MaxConnectionsPerServer = 256;

            // Now create the client itself with our options above
            _apiClient = new(options);
        }

        /// <summary>
        /// Runs every hour.
        /// </summary>
        [Function("CheckByondVersions")]
        public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo trigger) {
            _logger.LogInformation($"Invoked at {DateTime.Now}");

            HttpResponseMessage get_byond_ver_res = await _apiClient.GetAsync("https://secure.byond.com/download/version.txt");

            if (!get_byond_ver_res.IsSuccessStatusCode) {
                return; // Dont do anything if its not a success
            }

            string byond_ver_txt = await get_byond_ver_res.Content.ReadAsStringAsync();

            List<string> byond_vers = byond_ver_txt.Trim().Split("\n").ToList();

            if (byond_vers.Count == 0) {
                _logger.LogError($"No BYOND versions found - body content: {byond_ver_txt}");
                return;
            }

            string? stable_version = null;
            string? beta_version = null;

            if (byond_vers.Count == 1) {
                stable_version = byond_vers[0];
            } else if (byond_vers.Count == 2) {
                stable_version = byond_vers[0];
                beta_version = byond_vers[1];
            } else {
                _logger.LogError($"BYOND version had >2 versions - body content: {byond_ver_txt}");
                return;
            }

            // If both blank, we have problem
            if (string.IsNullOrWhiteSpace(stable_version) && string.IsNullOrWhiteSpace(beta_version)) {
                _logger.LogError($"BYOND was somehow not set - body content: {byond_ver_txt}");
            }

            bool generate_stable = false;
            bool generate_beta = false;

            // See which ones we need to generate
            Dictionary<ByondReleaseChannel, string> existing_channels = await _dataService.GetByondVersions();

            if (!string.IsNullOrWhiteSpace(stable_version)) {
                // See if we even have it from versions.txt
                if (existing_channels.ContainsKey(ByondReleaseChannel.Stable)) {
                    // See if its a mistmatch
                    if (existing_channels[ByondReleaseChannel.Stable] != stable_version) {
                        generate_stable = true;
                    }
                } else {
                    // We dont have stable stored - must generate it
                    generate_stable = true;
                }
            }

            // See if we even have it from versions.txt
            if (!string.IsNullOrWhiteSpace(beta_version)) {
                if (existing_channels.ContainsKey(ByondReleaseChannel.Beta)) {
                    // See if its a mistmatch
                    if (existing_channels[ByondReleaseChannel.Beta] != beta_version) {
                        generate_beta = true;
                    }
                } else {
                    // We dont have beta stored - must generate it
                    generate_beta = true;
                }
            }

            if (!generate_stable && !generate_beta) {
                _logger.LogInformation("No new updates - exiting");
                return;
            }

            // Only write if theres been a change - no point in it
            Dictionary<ByondReleaseChannel, string> versions_to_store = new();

            if (!string.IsNullOrWhiteSpace(stable_version)) {
                versions_to_store[ByondReleaseChannel.Stable] = stable_version;
            }

            if (!string.IsNullOrWhiteSpace(beta_version)) {
                versions_to_store[ByondReleaseChannel.Beta] = beta_version;
            }

            // Write it to the datastore
            await _dataService.WriteByondVersions(versions_to_store);

            // The extra isnull or whitespace check here is to shut the linter up
            if (generate_stable && !string.IsNullOrWhiteSpace(stable_version)) {
                DiscordWebhook? webhook = await GenerateWebhook(stable_version, ByondReleaseChannel.Stable);

                if (webhook == null) {
                    _logger.LogError($"Failed to generate stable changelog for {stable_version} - investigate");
                    return;
                }

                await SendWebhook(webhook);
            }

            if (generate_beta && !string.IsNullOrWhiteSpace(beta_version)) {
                DiscordWebhook? webhook = await GenerateWebhook(beta_version, ByondReleaseChannel.Beta);

                if (webhook == null) {
                    _logger.LogError($"Failed to generate beta changelog for {beta_version} - investigate");
                    return;
                }

                await SendWebhook(webhook);
            }
        }



        /// <summary>
        /// Parses the supplied version tag and generates a webhook from the page data.
        /// </summary>
        /// <param name="versionTag">The <see cref="string"/> BYOND major version tag.</param>
        /// <param name="channel">The <see cref="ByondReleaseChannel"/> channel this version is for.</param>
        /// <returns>A <see cref="DiscordWebhook"/> model populated with the info, or <see cref="null"/> if it failed.</returns>
        private async Task<DiscordWebhook?> GenerateWebhook(string versionTag, ByondReleaseChannel channel) {
            // Get the major version and CL URL
            string major_version = versionTag.Split(".")[0];
            string cl_url = $"https://secure.byond.com/docs/notes/{major_version}.html";

            // Lets get parsing that URL - stack thoes awaits baby
            string byond_cl_raw = await (await _apiClient.GetAsync(cl_url)).Content.ReadAsStringAsync();

            // Start parsing that out
            HtmlParser parser = new HtmlParser();
            IHtmlDocument document = parser.ParseDocument(byond_cl_raw);

            // Map of BYOND versions to their infos
            Dictionary<string, List<ByondInfoHolder>> info_map = new();

            // Stuff for current iterations
            string current_build = string.Empty;
            ByondInfoHolder? current_info = null;
            ByondInfoHolder.ApplicationChangelogHolder? current_application = null;

            // Now get the body content
            IElement? body_elem = document.QuerySelector("body");

            if (body_elem == null) {
                _logger.LogInformation($"No body on {cl_url}");
                return null;
            }

            List<IElement> elements = new(body_elem.Children);
            elements.RemoveAt(0); // Remove first thing - main header
            elements.RemoveAt(elements.Count - 1); // Remove last thing - footer

            // And begin the hellish parsing
            foreach (IElement element in elements) {
                if (element.TagName.ToLower() == "h3") {
                    // Get the version out
                    string version_key = element.TextContent.Split("Build ")[1].Trim();

                    // Assign our current stuff
                    current_build = version_key;

                    // Add to the info map
                    info_map.Add(version_key, new());
                }

                // Now try extract version info
                if (element.TagName.ToLower() == "p") {
                    if (string.IsNullOrWhiteSpace(element.TextContent)) {
                        continue; // Skip to next element to account for blank paragraphs
                    }

                    if (element.ChildElementCount == 2) {
                        // Program header - begin the parsing hell - so much sanity checking
                        IElement? link_name_elem = element.QuerySelector("u");
                        if (link_name_elem == null) {
                            if (element.TextContent.Contains("View All")) {
                                continue; // We are at the end of the page - stop caring
                            }
                            _logger.LogError($"[A1] Parsing error - URL: {cl_url}");
                            return null;
                        }

                        IElement? link_href_elem = element.QuerySelector("a");
                        if (link_href_elem == null) {
                            _logger.LogError($"[A2] Parsing error - URL: {cl_url}");
                            return null;
                        }

                        // Get the href out
                        string? link_href = link_href_elem.GetAttribute("href");
                        if (string.IsNullOrWhiteSpace(link_href)) {
                            _logger.LogError($"[A3] Parsing error - URL: {cl_url}");
                            return null;
                        }

                        if (string.IsNullOrWhiteSpace(current_build)) {
                            _logger.LogError($"[A4] current_build is somehow null - URL: {cl_url}");
                            return null;
                        }

                        if (!info_map.ContainsKey(current_build)) {
                            _logger.LogInformation($"[A5] Couldnt find version {current_build} in info map - URL: {cl_url}");
                            return null;
                        }

                        // Save our info
                        current_info = new();
                        current_info.OuterTypeName = link_name_elem.TextContent;
                        current_info.OuterTypeLink = link_href;
                        info_map[current_build].Add(current_info);
                    } else {
                        // Entry header - do shenanigans
                        string program_name = element.TextContent;
                        current_application = new() {
                            ApplicationName = program_name
                        };

                        // Get the next child - its the <ul> entry
                        IElement? next_child = element.NextElementSibling;

                        if (next_child == null) {
                            _logger.LogError($"[A6] No child after {element.TextContent} - URL: {cl_url}");
                            return null;
                        }

                        if (next_child.TagName.ToLower() != "ul") {
                            _logger.LogError($"[A7] Child after {element.TextContent} is not <ul> URL: {cl_url}");
                            return null;
                        }

                        // Parse children for CL entries
                        foreach (IElement li_elem in next_child.Children) {
                            if (li_elem.TagName.ToLower() == "li") {
                                current_application.Entries.Add(li_elem.TextContent.Trim());
                            }
                        }

                        // This should never happen?
                        if (current_info == null) {
                            _logger.LogError($"[A8] current_info is somehow null - URL: {cl_url}");
                            return null;
                        }

                        // Add it in
                        current_info.ApplicationEntries.Add(current_application);
                    }
                }
            }

            if (!info_map.ContainsKey(versionTag)) {
                _logger.LogInformation($"Couldnt find version {versionTag} in {cl_url}");
                return null;
            }

            // Then send it back
            return new DiscordWebhook(versionTag, channel, info_map[versionTag]);
        }



        /// <summary>
        /// Sends the webhook to all the hooks we have on file.
        /// </summary>
        /// <param name="webhookModel">The <see cref="DiscordWebhook"/> to send data to.</param>
        private async Task SendWebhook(DiscordWebhook webhookModel) {
            // Load the URLs
            List<string> hooks_to_post_to = await _dataService.GetWebhooks();

            // Sanity check
            if (hooks_to_post_to.Count == 0) {
                _logger.LogWarning("No webhooks to post to - this doesnt seem right");
                return;
            }

            // And post
            foreach (string hook_url in hooks_to_post_to) {
                await _apiClient.PostAsync(hook_url, JsonContent.Create(webhookModel));
            }
        }
    }
}
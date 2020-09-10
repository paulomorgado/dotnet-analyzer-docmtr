using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AnalyzerDocumenter.Writers
{
    internal sealed class ChecksWriter : WriterBase
    {
        private HttpClient httpClient;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. httpClient will be initialized after invoking WriteRuleAsync.
        public ChecksWriter(string filePath)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. httpClient will be initialized after invoking WriteRuleAsync.
            : base(filePath)
        {
        }

        protected internal override async Task WriteStartAnalyzerAsync(AssemblyDescriptor assemblyDescriptor)
        {
            await this.FileWriter.WriteAsync("# ");
            await this.FileWriter.WriteLineAsync(assemblyDescriptor.AssemblyName);
            await this.FileWriter.WriteLineAsync();
            await this.FileWriter.WriteLineAsync("|Rule ID | Title | Check |");
            await this.FileWriter.WriteLineAsync("|--------|-------|-------|");
            await this.FileWriter.WriteLineAsync();
        }

        protected internal override async Task WriteRuleAsync(RuleDescriptor rule)
        {
            string? error = null;

            if (string.IsNullOrEmpty(rule.Diagnostic.HelpLinkUri))
            {
                error = "Null or empty help link URI.";
            }
            else if (!Uri.TryCreate(rule.Diagnostic.HelpLinkUri, UriKind.Absolute, out var uri))
            {
                error = $"Invalid help link URI: {rule.Diagnostic.HelpLinkUri}";
            }
            else
            {
                this.httpClient ??= new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, })
                {
                    DefaultRequestHeaders =
                            {
                                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4255.0 Safari/537.36 Edg/87.0.634.0" },
                                { "Accept", "text/html,application/xhtml+xml,application/xml" },
                                { "Accept-Encoding", "gzip, deflate" },
                            },
                };

                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    using var response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.OK:
                            break;
                        case System.Net.HttpStatusCode.Moved when response.Headers.Location is Uri location:
                            error = $"Help link {uri} moved to {location}";
                            break;
                        default:
                            error = $"Invalid response ({((int)(response.StatusCode)).ToString()} - {response.StatusCode}) for help link URI: {rule.Diagnostic.HelpLinkUri}";
                            break;
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }

            if (!string.IsNullOrEmpty(error))
            {
                await this.FileWriter.WriteAsync("|");
                await this.FileWriter.WriteAsync(rule.Diagnostic.Id);
                await this.FileWriter.WriteAsync("|");
                await this.FileWriter.WriteAsync(rule.Diagnostic.Title.ToString());
                await this.FileWriter.WriteAsync("|");
                await this.FileWriter.WriteAsync(error);
                await this.FileWriter.WriteLineAsync("|");
            }
        }

        protected internal override async Task WriteEndAnalyzerAsync()
        {
            await this.FileWriter.WriteLineAsync();
        }
    }
}

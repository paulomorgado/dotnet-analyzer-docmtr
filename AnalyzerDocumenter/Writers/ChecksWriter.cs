using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
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

            if (!Uri.TryCreate(rule.Diagnostic.HelpLinkUri, UriKind.Absolute, out var uri))
            {
                error = $"Invalid help link URI: {rule.Diagnostic.HelpLinkUri}";
            }
            else
            {
                this.httpClient ??= new HttpClient();

                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Head, uri);
                    using var response = await this.httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        error = $"Invalid response ({response.StatusCode}) for link URI: {rule.Diagnostic.HelpLinkUri}";
                    }
                }
                catch
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

            if (!string.IsNullOrEmpty(error))
            {

            }
        }

        protected internal override async Task WriteEndAnalyzerAsync()
        {
            await this.FileWriter.WriteLineAsync();
        }
    }
}

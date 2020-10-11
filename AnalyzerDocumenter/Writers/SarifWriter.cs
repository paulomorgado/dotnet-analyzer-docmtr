using System;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AnalyzerDocumenter.Writers
{
    internal sealed class SarifWriter : JsonWriterBase
    {
        private static readonly CultureInfo culture = new CultureInfo("en-us");

        public SarifWriter(string filePath)
            : base(filePath)
        {
        }

        protected internal override async Task WriteStartAsync()
        {
            await base.WriteStartAsync();

            this.JsonWriter.WriteStartObject();
            this.JsonWriter.WriteString("$schema", "https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0-rtm.5.json");
            this.JsonWriter.WriteString("version", "2.1.0");
            this.JsonWriter.WriteStartArray("runs");
        }

        protected internal override Task WriteStartAnalyzerAsync(AssemblyDescriptor assemblyDescriptor)
        {
            this.JsonWriter.WriteStartObject();

            this.JsonWriter.WriteStartObject("tool");

            this.JsonWriter.WriteStartObject("driver");

            this.JsonWriter.WriteString("name", assemblyDescriptor.AssemblyName);

            if (!string.IsNullOrEmpty(assemblyDescriptor.Version))
            {
                this.JsonWriter.WriteString("version", assemblyDescriptor.Version);
            }

            if (!string.IsNullOrEmpty(assemblyDescriptor.DottedQuadFileVersion))
            {
                this.JsonWriter.WriteString("dottedQuadFileVersion", assemblyDescriptor.DottedQuadFileVersion);
            }

            if (!string.IsNullOrEmpty(assemblyDescriptor.SemanticVersion))
            {
                this.JsonWriter.WriteString("semanticVersion", assemblyDescriptor.SemanticVersion);
            }

            this.JsonWriter.WriteString("language", culture.Name);

            this.JsonWriter.WriteStartArray("rules");

            return Task.CompletedTask;
        }

        protected internal override Task WriteRuleAsync(RuleDescriptor rule)
        {
            this.JsonWriter.WriteStartObject();

            this.JsonWriter.WriteString("id", rule.Diagnostic.Id);

            var shortDescription = rule.Diagnostic.Title.ToString(culture);
            if (!string.IsNullOrEmpty(shortDescription))
            {
                this.JsonWriter.WriteStartObject("shortDescription");
                this.JsonWriter.WriteString("text", shortDescription);
                this.JsonWriter.WriteEndObject();
            }

            var fullDescription = rule.Diagnostic.Description.ToString(culture);
            if (!string.IsNullOrEmpty(fullDescription))
            {
                this.JsonWriter.WriteStartObject("fullDescription");
                this.JsonWriter.WriteString("text", fullDescription);
                this.JsonWriter.WriteEndObject();
            }

            this.JsonWriter.WriteStartObject("defaultConfiguration");
            this.JsonWriter.WriteString(
                "level",
                rule.Diagnostic.DefaultSeverity switch
                {
                    DiagnosticSeverity.Info => "note",
                    DiagnosticSeverity.Error => "error",
                    DiagnosticSeverity.Warning => "warning",
                    DiagnosticSeverity.Hidden => "hidden",
                    _ => "warning"
                });
            this.JsonWriter.WritePropertyName("enabled");
            this.JsonWriter.WriteBooleanValue(rule.Diagnostic.IsEnabledByDefault);
            this.JsonWriter.WriteEndObject();

            if (!string.IsNullOrEmpty(rule.Diagnostic.HelpLinkUri))
            {
                this.JsonWriter.WriteString("helpUri", rule.Diagnostic.HelpLinkUri);
            }

            this.JsonWriter.WriteStartObject("properties");

            if (!string.IsNullOrEmpty(rule.Diagnostic.Category))
            {
                this.JsonWriter.WriteString("category", rule.Diagnostic.Category);
            }

            using (var tagEnumerator = rule.Diagnostic.CustomTags.GetEnumerator())
            {
                if (tagEnumerator.MoveNext())
                {
                    this.JsonWriter.WriteStartArray("tags");

                    do
                    {
                        this.JsonWriter.WriteStringValue(tagEnumerator.Current);
                    }
                    while (tagEnumerator.MoveNext());

                    this.JsonWriter.WriteEndArray(); // tags
                }
            }

            this.JsonWriter.WriteString("typeName", rule.TypeName);

            if (rule.Languages.Length > 0)
            {
                this.JsonWriter.WriteStartArray("languages");

                foreach (var language in rule.Languages.OrderBy(l => l, StringComparer.InvariantCultureIgnoreCase))
                {
                    this.JsonWriter.WriteStringValue(language);
                }

                this.JsonWriter.WriteEndArray(); // languages
            }

            this.JsonWriter.WriteEndObject();

            this.JsonWriter.WriteEndObject();

            return Task.CompletedTask;
        }

        protected internal override Task WriteEndAnalyzerAsync()
        {
            this.JsonWriter.WriteEndArray();
            this.JsonWriter.WriteEndObject();
            this.JsonWriter.WriteEndObject();

            this.JsonWriter.WriteString("columnKind", "utf16CodeUnits");

            this.JsonWriter.WriteEndObject();

            return Task.CompletedTask;
        }

        protected internal override async Task WriteEndAsync()
        {
            this.JsonWriter.WriteEndArray();
            this.JsonWriter.WriteEndObject();

            await base.WriteEndAsync();
        }
    }
}

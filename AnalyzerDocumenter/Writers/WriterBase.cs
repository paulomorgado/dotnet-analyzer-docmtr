using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzerDocumenter.Writers
{
    internal abstract class WriterBase
    {
        //private static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

#pragma warning disable CS8618 // Non-nullable field is uninitialized. FileWriter will be initialized after invoking WriteStartAsync.
        protected WriterBase(string filePath)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. FileWriter will be initialized after invoking WriteStartAsync.
        {
            this.FilePath = filePath;
        }

        protected string FilePath { get; }

        protected StreamWriter FileWriter { get; set; }

        protected internal virtual async Task WriteStartAsync()
        {
            this.FileWriter = new StreamWriter(this.FilePath, false, Encoding.UTF8);

            await this.FileWriter.FlushAsync();
        }

        protected internal virtual Task WriteStartAnalyzerAsync(AssemblyDescriptor assemblyDescriptor)
        {
            return Task.CompletedTask;
        }

        protected internal virtual Task WriteStartRulesAsync(bool isSelectedRuleGroup)
        {
            return Task.CompletedTask;
        }

        protected internal abstract Task WriteRuleAsync(RuleDescriptor rule);

        protected internal virtual Task WriteEndRulesAsync()
        {
            return Task.CompletedTask;
        }

        protected internal virtual Task WriteEndAnalyzerAsync()
        {
            return Task.CompletedTask;
        }

        protected internal virtual async Task WriteEndAsync()
        {
            await this.FileWriter.DisposeAsync();
        }
    }
}

using System;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace AnalyzerDocumenter.Writers
{
    internal abstract class JsonWriterBase : WriterBase
    {
        private static readonly JsonWriterOptions jsonWriterOptions = new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
#pragma warning disable CS8618 // Non-nullable field is uninitialized. jsonWriter will be initialized after invoking WriteStartAsync.
        protected JsonWriterBase(string filePath)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. jsonWriter will be initialized after invoking WriteStartAsync.
            : base(filePath)
        {
        }

        protected Utf8JsonWriter JsonWriter { get; private set; }

        protected internal override async Task WriteStartAsync()
        {
            await base.WriteStartAsync();

            this.JsonWriter = new Utf8JsonWriter(this.FileWriter!.BaseStream, jsonWriterOptions);
        }

        protected internal override async Task WriteEndAsync()
        {
            await this.JsonWriter.DisposeAsync();
            await base.WriteEndAsync();
        }
    }
}

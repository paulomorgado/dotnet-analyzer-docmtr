using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CodeAnalysis;

namespace AnalyzerDocumenter.Writers
{
    internal abstract class XmlWriterBase : WriterBase
    {
        private static readonly XmlWriterSettings? xmlWriterSettings = new XmlWriterSettings { Indent = true, Async = true };
        private readonly bool shouldWriteStartDocument;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. xmlWriter will be initialized after invoking WriteStartAsync.
        protected XmlWriterBase(string filePath, bool shouldWriteStartDocument)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. xmlWriter will be initialized after invoking WriteStartAsync.
            : base(filePath)
        {
            this.shouldWriteStartDocument = shouldWriteStartDocument;
        }

        protected XmlWriter XmlWriter { get; private set; }

        protected internal override async Task WriteStartAsync()
        {
            await base.WriteStartAsync();

            this.XmlWriter = XmlWriter.Create(this.FileWriter!.BaseStream, xmlWriterSettings);

            if (this.shouldWriteStartDocument)
            {
                await this.XmlWriter.WriteStartDocumentAsync();
            }
        }

        protected internal override async Task WriteEndAsync()
        {
            await this.XmlWriter.WriteEndDocumentAsync();
            await this.XmlWriter.FlushAsync();
            this.XmlWriter.Dispose();

            await base.WriteEndAsync();
        }
    }
}

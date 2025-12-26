// PDFsharp - A .NET library for processing PDF
// See the LICENSE file in the solution root for more information.

using PdfSharp.Pdf.IO;

namespace PdfSharp.Pdf.Linearization
{
    /// <summary>
    /// Represents the hint stream dictionary for linearized PDFs.
    /// Contains the page offset and shared object hint tables.
    /// Reference: ISO 32000-1:2008, Table F.1
    /// </summary>
    internal sealed class PdfHintStream : PdfDictionary
    {
        readonly PdfPageOffsetHintTable _pageOffsetHintTable;
        readonly PdfSharedObjectHintTable _sharedObjectHintTable;
        byte[]? _encodedStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfHintStream"/> class.
        /// </summary>
        public PdfHintStream(PdfDocument document,
            PdfPageOffsetHintTable pageOffsetHintTable,
            PdfSharedObjectHintTable sharedObjectHintTable)
            : base(document)
        {
            _pageOffsetHintTable = pageOffsetHintTable;
            _sharedObjectHintTable = sharedObjectHintTable;
        }

        /// <summary>
        /// Gets the offset within the stream to the shared object hint table.
        /// </summary>
        public int SharedObjectHintTableOffset { get; private set; }

        /// <summary>
        /// Gets the length of the shared object hint table.
        /// </summary>
        public int SharedObjectHintTableLength { get; private set; }

        /// <summary>
        /// Encodes the hint tables and prepares the stream for writing.
        /// </summary>
        public void PrepareStream()
        {
            // Encode both hint tables
            byte[] pageOffsetData = _pageOffsetHintTable.Encode();
            byte[] sharedObjectData = _sharedObjectHintTable.Encode();

            // Store offset and length for the /S key
            SharedObjectHintTableOffset = pageOffsetData.Length;
            SharedObjectHintTableLength = sharedObjectData.Length;

            // Combine into single stream
            _encodedStream = new byte[pageOffsetData.Length + sharedObjectData.Length];
            Array.Copy(pageOffsetData, 0, _encodedStream, 0, pageOffsetData.Length);
            Array.Copy(sharedObjectData, 0, _encodedStream, pageOffsetData.Length, sharedObjectData.Length);

            // Set dictionary entries
            Elements.SetInteger(Keys.S, SharedObjectHintTableOffset);
            Elements.SetInteger("/Length", _encodedStream.Length);

            // Create the stream
            CreateStream(_encodedStream);
        }

        /// <summary>
        /// Predefined keys of the hint stream dictionary.
        /// </summary>
        internal class Keys : KeysBase
        {
            /// <summary>
            /// (Required) The offset in bytes from the beginning of this stream
            /// to the start of the shared object hint table.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Required)]
            public const string S = "/S";

            /// <summary>
            /// (Optional) The offset in bytes from the beginning of this stream
            /// to the start of the thumbnail hint table.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Optional)]
            public const string T = "/T";

            /// <summary>
            /// (Optional) The offset in bytes from the beginning of this stream
            /// to the start of the outline hint table.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Optional)]
            public const string O = "/O";

            /// <summary>
            /// (Optional) The offset in bytes from the beginning of this stream
            /// to the start of the article thread hint table.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Optional)]
            public const string A = "/A";

            /// <summary>
            /// (Optional) The offset in bytes from the beginning of this stream
            /// to the start of the named destination hint table.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Optional)]
            public const string E = "/E";

            /// <summary>
            /// (Optional) The offset in bytes from the beginning of this stream
            /// to the start of the interactive form hint table.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Optional)]
            public const string V = "/V";

            /// <summary>
            /// (Optional) The offset in bytes from the beginning of this stream
            /// to the start of the logical structure hint table.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Optional)]
            public const string I = "/I";

            /// <summary>
            /// (Optional) The offset in bytes from the beginning of this stream
            /// to the start of the page labels hint table.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Optional)]
            public const string C = "/C";

            /// <summary>
            /// (Optional) The offset in bytes from the beginning of this stream
            /// to the start of the renditions hint table.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Optional)]
            public const string R = "/R";

            /// <summary>
            /// (Optional) The offset in bytes from the beginning of this stream
            /// to the start of the embedded files hint table.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Optional)]
            public const string B = "/B";

            /// <summary>
            /// Gets the KeysMeta for these keys.
            /// </summary>
            public static DictionaryMeta Meta => _meta ??= CreateMeta(typeof(Keys));

            static DictionaryMeta? _meta;
        }

        /// <summary>
        /// Gets the KeysMeta of this dictionary type.
        /// </summary>
        internal override DictionaryMeta Meta => Keys.Meta;
    }
}

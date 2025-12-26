// PDFsharp - A .NET library for processing PDF
// See the LICENSE file in the solution root for more information.

using PdfSharp.Pdf.IO;

namespace PdfSharp.Pdf.Linearization
{
    /// <summary>
    /// Represents the linearization parameter dictionary.
    /// This dictionary must be the first indirect object in a linearized PDF file.
    /// Reference: ISO 32000-1:2008, Annex F (Linearized PDF)
    /// </summary>
    internal sealed class PdfLinearizationDictionary : PdfDictionary
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PdfLinearizationDictionary"/> class.
        /// </summary>
        public PdfLinearizationDictionary(PdfDocument document)
            : base(document)
        {
            Elements.SetReal(Keys.Linearized, 1.0);
        }

        /// <summary>
        /// Gets or sets the total length of the linearized PDF file in bytes.
        /// </summary>
        public long FileLength
        {
            get => Elements.GetInteger(Keys.L);
            set => Elements.SetInteger(Keys.L, (int)value);
        }

        /// <summary>
        /// Gets or sets the hint stream location as an array [offset, length].
        /// </summary>
        public void SetHintStreamLocation(long offset, long length)
        {
            var array = new PdfArray(_document);
            array.Elements.Add(new PdfInteger((int)offset));
            array.Elements.Add(new PdfInteger((int)length));
            Elements[Keys.H] = array;
        }

        /// <summary>
        /// Gets or sets the object number of the first page's page object.
        /// </summary>
        public int FirstPageObjectNumber
        {
            get => Elements.GetInteger(Keys.O);
            set => Elements.SetInteger(Keys.O, value);
        }

        /// <summary>
        /// Gets or sets the offset of the end of the first page section.
        /// </summary>
        public long EndOfFirstPage
        {
            get => Elements.GetInteger(Keys.E);
            set => Elements.SetInteger(Keys.E, (int)value);
        }

        /// <summary>
        /// Gets or sets the number of pages in the document.
        /// </summary>
        public int PageCount
        {
            get => Elements.GetInteger(Keys.N);
            set => Elements.SetInteger(Keys.N, value);
        }

        /// <summary>
        /// Gets or sets the offset of the main cross-reference table
        /// (the one at the end of the file, not the first-page xref).
        /// </summary>
        public long MainXRefOffset
        {
            get => Elements.GetInteger(Keys.T);
            set => Elements.SetInteger(Keys.T, (int)value);
        }

        /// <summary>
        /// Predefined keys of the linearization parameter dictionary.
        /// </summary>
        internal class Keys : KeysBase
        {
            /// <summary>
            /// (Required) A version identification for the linearized format.
            /// </summary>
            [KeyInfo(KeyType.Real | KeyType.Required)]
            public const string Linearized = "/Linearized";

            /// <summary>
            /// (Required) The length of the entire file in bytes.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Required)]
            public const string L = "/L";

            /// <summary>
            /// (Required) An array of two integers specifying the offset and length
            /// of the primary hint stream.
            /// </summary>
            [KeyInfo(KeyType.Array | KeyType.Required)]
            public const string H = "/H";

            /// <summary>
            /// (Required) The object number of the first page's page object.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Required)]
            public const string O = "/O";

            /// <summary>
            /// (Required) The offset of the end of the first page.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Required)]
            public const string E = "/E";

            /// <summary>
            /// (Required) The number of pages in the document.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Required)]
            public const string N = "/N";

            /// <summary>
            /// (Required) The offset of the white-space character preceding the first
            /// entry of the main cross-reference table.
            /// </summary>
            [KeyInfo(KeyType.Integer | KeyType.Required)]
            public const string T = "/T";

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

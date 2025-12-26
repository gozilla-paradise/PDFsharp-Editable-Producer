// PDFsharp - A .NET library for processing PDF
// See the LICENSE file in the solution root for more information.

namespace PdfSharp.Pdf.Linearization
{
    /// <summary>
    /// Represents the page offset hint table for linearized PDFs.
    /// This table allows readers to quickly locate any page in the document.
    /// Reference: ISO 32000-1:2008, Table F.3
    /// </summary>
    internal sealed class PdfPageOffsetHintTable
    {
        /// <summary>
        /// Per-page entry information.
        /// </summary>
        internal sealed class PageEntry
        {
            /// <summary>
            /// Number of objects in the page (delta from minimum).
            /// </summary>
            public int ObjectCountDelta { get; set; }

            /// <summary>
            /// Page length in bytes (delta from minimum).
            /// </summary>
            public long PageLengthDelta { get; set; }

            /// <summary>
            /// Number of shared object references (delta from minimum).
            /// </summary>
            public int SharedObjectCountDelta { get; set; }

            /// <summary>
            /// Indices of shared objects referenced by this page.
            /// </summary>
            public int[] SharedObjectIndices { get; set; } = [];

            /// <summary>
            /// Number of content stream objects (delta from minimum).
            /// </summary>
            public int ContentStreamCountDelta { get; set; }

            /// <summary>
            /// Content stream length in bytes (delta from minimum).
            /// </summary>
            public long ContentStreamLengthDelta { get; set; }
        }

        // Header values (ISO 32000-1:2008, Table F.3, items 1-13)

        /// <summary>
        /// Item 1: The least number of objects in a page.
        /// </summary>
        public int MinObjectsPerPage { get; set; } = 1;

        /// <summary>
        /// Item 2: Location of the first page's page object.
        /// </summary>
        public long FirstPageObjectOffset { get; set; }

        /// <summary>
        /// Item 3: Number of bits needed to represent the difference between
        /// the greatest and least number of objects in a page.
        /// </summary>
        public int BitsForObjectCountDelta { get; set; }

        /// <summary>
        /// Item 4: The least page length in bytes.
        /// </summary>
        public long MinPageLength { get; set; }

        /// <summary>
        /// Item 5: Number of bits needed to represent the difference between
        /// the greatest and least page length.
        /// </summary>
        public int BitsForPageLengthDelta { get; set; }

        /// <summary>
        /// Item 6: The least offset to the start of a content stream.
        /// </summary>
        public long MinContentStreamOffset { get; set; }

        /// <summary>
        /// Item 7: Number of bits needed to represent the difference between
        /// the greatest and least content stream offset.
        /// </summary>
        public int BitsForContentStreamOffsetDelta { get; set; }

        /// <summary>
        /// Item 8: The least content stream length.
        /// </summary>
        public long MinContentStreamLength { get; set; }

        /// <summary>
        /// Item 9: Number of bits needed to represent the difference between
        /// the greatest and least content stream length.
        /// </summary>
        public int BitsForContentStreamLengthDelta { get; set; }

        /// <summary>
        /// Item 10: Number of bits needed to represent the greatest number
        /// of shared object references.
        /// </summary>
        public int BitsForSharedObjectCount { get; set; }

        /// <summary>
        /// Item 11: Number of bits needed to represent the numerically greatest
        /// shared object identifier used by any page.
        /// </summary>
        public int BitsForSharedObjectId { get; set; }

        /// <summary>
        /// Item 12: Number of bits needed to represent the numerator of the
        /// fractional position for each shared object reference.
        /// </summary>
        public int BitsForFractionalPosition { get; set; }

        /// <summary>
        /// Item 13: The denominator of the fractional position for each
        /// shared object reference.
        /// </summary>
        public int FractionalPositionDenominator { get; set; } = 1;

        /// <summary>
        /// Per-page entries.
        /// </summary>
        public List<PageEntry> PageEntries { get; } = new();

        /// <summary>
        /// Encodes the hint table to a byte array.
        /// </summary>
        public byte[] Encode()
        {
            using var stream = new MemoryStream();
            using var writer = new BitWriter(stream);

            // Write header (32-bit values)
            writer.Write32(MinObjectsPerPage);
            writer.Write32((int)FirstPageObjectOffset);
            writer.Write16(BitsForObjectCountDelta);
            writer.Write32((int)MinPageLength);
            writer.Write16(BitsForPageLengthDelta);
            writer.Write32((int)MinContentStreamOffset);
            writer.Write16(BitsForContentStreamOffsetDelta);
            writer.Write32((int)MinContentStreamLength);
            writer.Write16(BitsForContentStreamLengthDelta);
            writer.Write16(BitsForSharedObjectCount);
            writer.Write16(BitsForSharedObjectId);
            writer.Write16(BitsForFractionalPosition);
            writer.Write16(FractionalPositionDenominator);

            // Write per-page entries
            // Item 1: Object count deltas
            foreach (var entry in PageEntries)
                writer.WriteBits(entry.ObjectCountDelta, BitsForObjectCountDelta);

            // Item 2: Page length deltas
            foreach (var entry in PageEntries)
                writer.WriteBits((int)entry.PageLengthDelta, BitsForPageLengthDelta);

            // Item 3: Shared object count deltas
            foreach (var entry in PageEntries)
                writer.WriteBits(entry.SharedObjectCountDelta, BitsForSharedObjectCount);

            // Item 4: Shared object identifiers (for each page, list of shared object indices)
            foreach (var entry in PageEntries)
            {
                foreach (var idx in entry.SharedObjectIndices)
                    writer.WriteBits(idx, BitsForSharedObjectId);
            }

            // Item 5: Fractional positions (we use 0 for simplicity)
            foreach (var entry in PageEntries)
            {
                for (int i = 0; i < entry.SharedObjectIndices.Length; i++)
                    writer.WriteBits(0, BitsForFractionalPosition);
            }

            // Item 6: Content stream count deltas
            foreach (var entry in PageEntries)
                writer.WriteBits(entry.ContentStreamCountDelta, BitsForObjectCountDelta);

            // Item 7: Content stream length deltas
            foreach (var entry in PageEntries)
                writer.WriteBits((int)entry.ContentStreamLengthDelta, BitsForContentStreamLengthDelta);

            writer.Flush();
            return stream.ToArray();
        }

        /// <summary>
        /// Helper class for writing bits to a stream.
        /// </summary>
        sealed class BitWriter : IDisposable
        {
            readonly Stream _stream;
            int _buffer;
            int _bitsInBuffer;

            public BitWriter(Stream stream)
            {
                _stream = stream;
            }

            public void Write16(int value)
            {
                Flush();
                _stream.WriteByte((byte)(value >> 8));
                _stream.WriteByte((byte)value);
            }

            public void Write32(int value)
            {
                Flush();
                _stream.WriteByte((byte)(value >> 24));
                _stream.WriteByte((byte)(value >> 16));
                _stream.WriteByte((byte)(value >> 8));
                _stream.WriteByte((byte)value);
            }

            public void WriteBits(int value, int bitCount)
            {
                if (bitCount <= 0)
                    return;

                // Add bits to buffer
                _buffer = (_buffer << bitCount) | (value & ((1 << bitCount) - 1));
                _bitsInBuffer += bitCount;

                // Write complete bytes
                while (_bitsInBuffer >= 8)
                {
                    _bitsInBuffer -= 8;
                    _stream.WriteByte((byte)(_buffer >> _bitsInBuffer));
                    _buffer &= (1 << _bitsInBuffer) - 1;
                }
            }

            public void Flush()
            {
                if (_bitsInBuffer > 0)
                {
                    // Pad remaining bits with zeros and write
                    _stream.WriteByte((byte)(_buffer << (8 - _bitsInBuffer)));
                    _buffer = 0;
                    _bitsInBuffer = 0;
                }
            }

            public void Dispose()
            {
                Flush();
            }
        }
    }
}

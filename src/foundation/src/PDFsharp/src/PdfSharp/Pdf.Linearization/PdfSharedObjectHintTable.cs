// PDFsharp - A .NET library for processing PDF
// See the LICENSE file in the solution root for more information.

namespace PdfSharp.Pdf.Linearization
{
    /// <summary>
    /// Represents the shared object hint table for linearized PDFs.
    /// This table provides information about objects shared between pages.
    /// Reference: ISO 32000-1:2008, Table F.5
    /// </summary>
    internal sealed class PdfSharedObjectHintTable
    {
        /// <summary>
        /// Per-shared-object entry information.
        /// </summary>
        internal sealed class SharedObjectEntry
        {
            /// <summary>
            /// Object length in bytes (delta from minimum).
            /// </summary>
            public int ObjectLengthDelta { get; set; }

            /// <summary>
            /// Whether this object is a signature.
            /// </summary>
            public bool IsSignature { get; set; }
        }

        // Header values (ISO 32000-1:2008, Table F.5, items 1-6)

        /// <summary>
        /// Item 1: Object number of the first object in the shared objects section.
        /// </summary>
        public int FirstSharedObjectNumber { get; set; }

        /// <summary>
        /// Item 2: Location of the first shared object.
        /// </summary>
        public long FirstSharedObjectOffset { get; set; }

        /// <summary>
        /// Item 3: Number of shared object entries for the first page.
        /// </summary>
        public int FirstPageSharedObjectCount { get; set; }

        /// <summary>
        /// Item 4: Number of shared object entries for all remaining pages.
        /// </summary>
        public int RemainingPagesSharedObjectCount { get; set; }

        /// <summary>
        /// Item 5: Least length of a shared object group in bytes.
        /// </summary>
        public int MinSharedObjectLength { get; set; }

        /// <summary>
        /// Item 6: Number of bits needed to represent the difference between
        /// the greatest and least length of a shared object group.
        /// </summary>
        public int BitsForSharedObjectLengthDelta { get; set; }

        /// <summary>
        /// Per-shared-object entries.
        /// </summary>
        public List<SharedObjectEntry> SharedObjectEntries { get; } = new();

        /// <summary>
        /// Encodes the hint table to a byte array.
        /// </summary>
        public byte[] Encode()
        {
            using var stream = new MemoryStream();
            using var writer = new BitWriter(stream);

            // Write header (32-bit values for offsets, 16-bit for others)
            writer.Write32(FirstSharedObjectNumber);
            writer.Write32((int)FirstSharedObjectOffset);
            writer.Write32(FirstPageSharedObjectCount);
            writer.Write32(RemainingPagesSharedObjectCount);
            writer.Write32(MinSharedObjectLength);
            writer.Write16(BitsForSharedObjectLengthDelta);

            // Write per-object entries
            // Item 1: Object length deltas
            foreach (var entry in SharedObjectEntries)
                writer.WriteBits(entry.ObjectLengthDelta, BitsForSharedObjectLengthDelta);

            // Item 2: Signature flags (1 bit each)
            foreach (var entry in SharedObjectEntries)
                writer.WriteBits(entry.IsSignature ? 1 : 0, 1);

            // Item 3: Number of objects in group (we use 1 for simplicity - no grouping)
            foreach (var _ in SharedObjectEntries)
                writer.WriteBits(0, 1); // 0 means 1 object per group

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

                _buffer = (_buffer << bitCount) | (value & ((1 << bitCount) - 1));
                _bitsInBuffer += bitCount;

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

// PDFsharp - A .NET library for processing PDF
// See the LICENSE file in the solution root for more information.

using System.Text;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace PdfSharp.Pdf.Linearization
{
    /// <summary>
    /// Writes a linearized PDF document.
    /// Uses a two-pass approach: first pass calculates sizes, second pass writes actual content.
    /// </summary>
    internal sealed class LinearizedPdfWriter
    {
        readonly PdfDocument _document;
        readonly PdfWriter _writer;

        // Collected object sets
        LinearizedObjectSets _objectSets = null!;

        // Linearization objects
        PdfLinearizationDictionary _linearizationDict = null!;
        PdfHintStream _hintStream = null!;

        // Calculated offsets and sizes
        readonly Dictionary<PdfObjectID, long> _objectSizes = new();
        readonly Dictionary<PdfObjectID, long> _objectPositions = new();

        // Structure offsets
        long _firstPageXRefOffset;
        long _firstPageXRefSize;
        long _endOfFirstPageSection;
        long _hintStreamOffset;
        long _hintStreamSize;
        long _mainXRefOffset;
        long _totalFileSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearizedPdfWriter"/> class.
        /// </summary>
        public LinearizedPdfWriter(PdfDocument document, PdfWriter writer)
        {
            _document = document;
            _writer = writer;
        }

        /// <summary>
        /// Writes the linearized PDF document.
        /// </summary>
        public async Task WriteAsync()
        {
            // Step 1: Collect and categorize objects
            var collector = new LinearizationObjectCollector(_document);
            _objectSets = collector.Collect();

            // Step 2: Create linearization dictionary and hint stream
            CreateLinearizationObjects();

            // Step 3: Pass 1 - Calculate all object sizes
            CalculateObjectSizes();

            // Step 4: Calculate structure offsets
            CalculateOffsets();

            // Step 5: Build hint tables with calculated offsets
            BuildHintTables();

            // Step 6: Pass 2 - Write actual content
            await WriteLinearizedContentAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the linearization dictionary and hint stream objects.
        /// </summary>
        void CreateLinearizationObjects()
        {
            // Create linearization dictionary
            _linearizationDict = new PdfLinearizationDictionary(_document);
            _document.IrefTable.Add(_linearizationDict);
            _objectSets.LinearizationDictionary = _linearizationDict.Reference;

            // Set known values
            _linearizationDict.PageCount = _document.Pages.Count;

            // First page object number
            if (_objectSets.FirstPageObjects.Length > 0)
            {
                // Find the page object itself (not just any object in the closure)
                var firstPage = _document.Pages[0];
                _linearizationDict.FirstPageObjectNumber = firstPage.ObjectNumber;
            }

            // Create hint stream (will be populated later)
            var pageOffsetHintTable = new PdfPageOffsetHintTable();
            var sharedObjectHintTable = new PdfSharedObjectHintTable();
            _hintStream = new PdfHintStream(_document, pageOffsetHintTable, sharedObjectHintTable);
            _document.IrefTable.Add(_hintStream);
            _objectSets.HintStream = _hintStream.Reference;
        }

        /// <summary>
        /// Pass 1: Calculate sizes of all objects by writing them to a memory stream.
        /// </summary>
        void CalculateObjectSizes()
        {
            using var measureStream = new MemoryStream();
            var measureWriter = new PdfWriter(measureStream, _document, null);
            measureWriter.Layout = PdfWriterLayout.Compact; // Use compact for accurate size measurement

            foreach (var iref in _objectSets.GetAllReferencesInOrder())
            {
                if (iref?.Value == null)
                    continue;

                long startPos = measureStream.Position;
                iref.Value.WriteObject(measureWriter);
                long endPos = measureStream.Position;

                _objectSizes[iref.ObjectID] = endPos - startPos;
            }

            // Also measure the xref table and trailer sizes
            measureStream.SetLength(0);
            measureStream.Position = 0;

            // Measure first-page xref (subset of objects)
            var firstPageXRefObjects = GetFirstPageXRefObjects();
            WritePartialXRef(measureWriter, firstPageXRefObjects, 0);
            measureWriter.WriteRaw("trailer\n");
            WriteFirstPageTrailer(measureWriter, 0); // Placeholder offset
            measureWriter.WriteRaw("\nstartxref\n0\n%%EOF\n");
            _firstPageXRefSize = measureStream.Position;
        }

        /// <summary>
        /// Calculates file structure offsets based on object sizes.
        /// </summary>
        void CalculateOffsets()
        {
            long currentOffset = 0;

            // Header size (approximate)
            int headerSize = GetHeaderSize();
            currentOffset += headerSize;

            // Linearization dictionary (must be object 1)
            if (_objectSets.LinearizationDictionary != null)
            {
                _objectPositions[_objectSets.LinearizationDictionary.ObjectID] = currentOffset;
                currentOffset += _objectSizes.GetValueOrDefault(_objectSets.LinearizationDictionary.ObjectID, 100);
            }

            // First-page xref section
            _firstPageXRefOffset = currentOffset;
            currentOffset += _firstPageXRefSize;

            // Document-level objects
            foreach (var iref in _objectSets.DocumentLevelObjects)
            {
                _objectPositions[iref.ObjectID] = currentOffset;
                currentOffset += _objectSizes.GetValueOrDefault(iref.ObjectID, 50);
            }

            // First page objects
            foreach (var iref in _objectSets.FirstPageObjects)
            {
                _objectPositions[iref.ObjectID] = currentOffset;
                currentOffset += _objectSizes.GetValueOrDefault(iref.ObjectID, 50);
            }

            _endOfFirstPageSection = currentOffset;

            // Hint stream
            if (_objectSets.HintStream != null)
            {
                _hintStreamOffset = currentOffset;
                // Hint stream size will be calculated after building hint tables
                // Use estimate for now
                _hintStreamSize = 200 + (_document.Pages.Count * 20) + (_objectSets.SharedObjects.Length * 10);
                _objectPositions[_objectSets.HintStream.ObjectID] = currentOffset;
                currentOffset += _hintStreamSize;
            }

            // Remaining pages
            foreach (var pageObjects in _objectSets.RemainingPageObjects)
            {
                foreach (var iref in pageObjects)
                {
                    _objectPositions[iref.ObjectID] = currentOffset;
                    currentOffset += _objectSizes.GetValueOrDefault(iref.ObjectID, 50);
                }
            }

            // Shared objects
            foreach (var iref in _objectSets.SharedObjects)
            {
                _objectPositions[iref.ObjectID] = currentOffset;
                currentOffset += _objectSizes.GetValueOrDefault(iref.ObjectID, 50);
            }

            // Main xref section
            _mainXRefOffset = currentOffset;

            // Calculate total file size (main xref + trailer + eof)
            int mainXRefSize = EstimateMainXRefSize();
            _totalFileSize = currentOffset + mainXRefSize;

            // Update linearization dictionary with calculated values
            _linearizationDict.FileLength = _totalFileSize;
            _linearizationDict.EndOfFirstPage = _endOfFirstPageSection;
            _linearizationDict.MainXRefOffset = _mainXRefOffset;
            _linearizationDict.SetHintStreamLocation(_hintStreamOffset, _hintStreamSize);
        }

        /// <summary>
        /// Builds the hint tables with the calculated offsets.
        /// </summary>
        void BuildHintTables()
        {
            var pageOffsetTable = new PdfPageOffsetHintTable();
            var sharedObjectTable = new PdfSharedObjectHintTable();

            // Build page offset hint table
            int pageCount = _document.Pages.Count;

            // Calculate min values
            int minObjects = int.MaxValue;
            long minPageLength = long.MaxValue;
            long minContentLength = long.MaxValue;

            var pageLengths = new long[pageCount];
            var pageObjectCounts = new int[pageCount];

            // First page
            pageObjectCounts[0] = _objectSets.DocumentLevelObjects.Length + _objectSets.FirstPageObjects.Length;
            pageLengths[0] = _endOfFirstPageSection - _firstPageXRefOffset - _firstPageXRefSize;

            // Remaining pages
            for (int i = 0; i < _objectSets.RemainingPageObjects.Length; i++)
            {
                pageObjectCounts[i + 1] = _objectSets.RemainingPageObjects[i].Length;
                pageLengths[i + 1] = _objectSets.RemainingPageObjects[i].Sum(r => _objectSizes.GetValueOrDefault(r.ObjectID, 0));
            }

            foreach (var count in pageObjectCounts)
                if (count < minObjects) minObjects = count;
            foreach (var length in pageLengths)
                if (length < minPageLength) minPageLength = length;

            if (minObjects == int.MaxValue) minObjects = 1;
            if (minPageLength == long.MaxValue) minPageLength = 0;
            if (minContentLength == long.MaxValue) minContentLength = 0;

            pageOffsetTable.MinObjectsPerPage = minObjects;
            pageOffsetTable.MinPageLength = minPageLength;
            pageOffsetTable.MinContentStreamLength = 0;
            pageOffsetTable.FirstPageObjectOffset = _firstPageXRefOffset + _firstPageXRefSize;

            // Calculate bits needed
            int maxObjectDelta = pageObjectCounts.Max() - minObjects;
            long maxLengthDelta = pageLengths.Max() - minPageLength;

            pageOffsetTable.BitsForObjectCountDelta = BitsNeeded(maxObjectDelta);
            pageOffsetTable.BitsForPageLengthDelta = BitsNeeded((int)maxLengthDelta);
            pageOffsetTable.BitsForContentStreamOffsetDelta = 1;
            pageOffsetTable.BitsForContentStreamLengthDelta = 1;
            pageOffsetTable.BitsForSharedObjectCount = BitsNeeded(_objectSets.SharedObjects.Length);
            pageOffsetTable.BitsForSharedObjectId = BitsNeeded(_objectSets.SharedObjects.Length);
            pageOffsetTable.BitsForFractionalPosition = 0;

            // Add page entries
            for (int i = 0; i < pageCount; i++)
            {
                var entry = new PdfPageOffsetHintTable.PageEntry
                {
                    ObjectCountDelta = pageObjectCounts[i] - minObjects,
                    PageLengthDelta = pageLengths[i] - minPageLength,
                    SharedObjectCountDelta = 0, // Simplified: no shared object references per page
                    SharedObjectIndices = [],
                    ContentStreamCountDelta = 0,
                    ContentStreamLengthDelta = 0
                };
                pageOffsetTable.PageEntries.Add(entry);
            }

            // Build shared object hint table
            if (_objectSets.SharedObjects.Length > 0)
            {
                var firstShared = _objectSets.SharedObjects[0];
                sharedObjectTable.FirstSharedObjectNumber = firstShared.ObjectNumber;
                sharedObjectTable.FirstSharedObjectOffset = _objectPositions.GetValueOrDefault(firstShared.ObjectID, 0);
                sharedObjectTable.FirstPageSharedObjectCount = 0;
                sharedObjectTable.RemainingPagesSharedObjectCount = _objectSets.SharedObjects.Length;

                int minSharedLength = int.MaxValue;
                int maxSharedLength = 0;
                foreach (var iref in _objectSets.SharedObjects)
                {
                    int len = (int)_objectSizes.GetValueOrDefault(iref.ObjectID, 0);
                    if (len < minSharedLength) minSharedLength = len;
                    if (len > maxSharedLength) maxSharedLength = len;
                }

                if (minSharedLength == int.MaxValue) minSharedLength = 0;

                sharedObjectTable.MinSharedObjectLength = minSharedLength;
                sharedObjectTable.BitsForSharedObjectLengthDelta = BitsNeeded(maxSharedLength - minSharedLength);

                foreach (var iref in _objectSets.SharedObjects)
                {
                    var entry = new PdfSharedObjectHintTable.SharedObjectEntry
                    {
                        ObjectLengthDelta = (int)_objectSizes.GetValueOrDefault(iref.ObjectID, 0) - minSharedLength,
                        IsSignature = false
                    };
                    sharedObjectTable.SharedObjectEntries.Add(entry);
                }
            }

            // Create new hint stream with populated tables
            _hintStream = new PdfHintStream(_document, pageOffsetTable, sharedObjectTable);
            _hintStream.PrepareStream();

            // Update the hint stream in object sets
            if (_objectSets.HintStream != null)
            {
                // Replace the hint stream reference value
                _objectSets.HintStream.Value = _hintStream;
            }
        }

        /// <summary>
        /// Pass 2: Write the actual linearized content.
        /// </summary>
        async Task WriteLinearizedContentAsync()
        {
            // Renumber objects in linearized order
            RenumberObjects();

            // Write header
            _writer.WriteFileHeader(_document);

            // Write linearization dictionary
            if (_objectSets.LinearizationDictionary != null)
            {
                _objectSets.LinearizationDictionary.Position = _writer.Position;
                _objectSets.LinearizationDictionary.Value.WriteObject(_writer);
            }

            // Write first-page xref section
            var firstPageObjects = GetFirstPageXRefObjects();
            WritePartialXRef(_writer, firstPageObjects, _mainXRefOffset);
            _writer.WriteRaw("trailer\n");
            WriteFirstPageTrailer(_writer, _mainXRefOffset);
            _writer.WriteRaw("\nstartxref\n");
            _writer.WriteRaw(_firstPageXRefOffset.ToString());
            _writer.WriteRaw("\n%%EOF\n");

            // Write document-level objects
            foreach (var iref in _objectSets.DocumentLevelObjects)
            {
                iref.Position = _writer.Position;
                iref.Value.WriteObject(_writer);
            }

            // Write first page objects
            foreach (var iref in _objectSets.FirstPageObjects)
            {
                iref.Position = _writer.Position;
                iref.Value.WriteObject(_writer);
            }

            // Write hint stream
            if (_objectSets.HintStream != null)
            {
                _objectSets.HintStream.Position = _writer.Position;
                _hintStream.WriteObject(_writer);
            }

            // Write remaining pages
            foreach (var pageObjects in _objectSets.RemainingPageObjects)
            {
                foreach (var iref in pageObjects)
                {
                    iref.Position = _writer.Position;
                    iref.Value.WriteObject(_writer);
                }
            }

            // Write shared objects
            foreach (var iref in _objectSets.SharedObjects)
            {
                iref.Position = _writer.Position;
                iref.Value.WriteObject(_writer);
            }

            // Write main xref section
            var mainXRefOffset = _writer.Position;
            WriteMainXRef(_writer);
            _writer.WriteRaw("trailer\n");
            WriteMainTrailer(_writer);
            _writer.WriteEof(_document, mainXRefOffset);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Renumbers objects in linearized order.
        /// </summary>
        void RenumberObjects()
        {
            int objectNumber = 1;

            // Linearization dictionary must be object 1
            if (_objectSets.LinearizationDictionary?.Value != null)
            {
                _objectSets.LinearizationDictionary.ObjectID = new PdfObjectID(objectNumber++);
            }

            // Document-level objects
            foreach (var iref in _objectSets.DocumentLevelObjects)
            {
                iref.ObjectID = new PdfObjectID(objectNumber++);
            }

            // First page objects
            foreach (var iref in _objectSets.FirstPageObjects)
            {
                iref.ObjectID = new PdfObjectID(objectNumber++);
            }

            // Hint stream
            if (_objectSets.HintStream != null)
            {
                _objectSets.HintStream.ObjectID = new PdfObjectID(objectNumber++);
            }

            // Remaining pages
            foreach (var pageObjects in _objectSets.RemainingPageObjects)
            {
                foreach (var iref in pageObjects)
                {
                    iref.ObjectID = new PdfObjectID(objectNumber++);
                }
            }

            // Shared objects
            foreach (var iref in _objectSets.SharedObjects)
            {
                iref.ObjectID = new PdfObjectID(objectNumber++);
            }

            _document.IrefTable.MaxObjectNumber = objectNumber - 1;
        }

        /// <summary>
        /// Gets the objects to include in the first-page xref section.
        /// </summary>
        PdfReference[] GetFirstPageXRefObjects()
        {
            var objects = new List<PdfReference>();

            if (_objectSets.LinearizationDictionary != null)
                objects.Add(_objectSets.LinearizationDictionary);

            objects.AddRange(_objectSets.DocumentLevelObjects);
            objects.AddRange(_objectSets.FirstPageObjects);

            if (_objectSets.HintStream != null)
                objects.Add(_objectSets.HintStream);

            return objects.ToArray();
        }

        /// <summary>
        /// Writes a partial xref table for a subset of objects.
        /// </summary>
        void WritePartialXRef(PdfWriter writer, PdfReference[] objects, long prevOffset)
        {
            writer.WriteRaw("xref\n");

            if (objects.Length == 0)
            {
                writer.WriteRaw("0 1\n");
                writer.WriteRaw("0000000000 65535 f \n");
                return;
            }

            // Sort by object number
            var sorted = objects.OrderBy(r => r.ObjectNumber).ToArray();

            // Write subsections
            writer.WriteRaw($"0 {sorted.Max(r => r.ObjectNumber) + 1}\n");

            // Free object entry
            writer.WriteRaw("0000000000 65535 f \n");

            // Write entries for each object, filling gaps with free entries
            int lastObjNum = 0;
            foreach (var iref in sorted)
            {
                // Fill gaps with free entries
                for (int i = lastObjNum + 1; i < iref.ObjectNumber; i++)
                {
                    writer.WriteRaw("0000000000 00000 f \n");
                }

                // Write the used entry
                writer.WriteRaw($"{iref.Position:D10} {iref.GenerationNumber:D5} n \n");
                lastObjNum = iref.ObjectNumber;
            }
        }

        /// <summary>
        /// Writes the first-page trailer.
        /// </summary>
        void WriteFirstPageTrailer(PdfWriter writer, long prevOffset)
        {
            var trailer = new StringBuilder();
            trailer.Append("<<");
            trailer.Append($"/Size {_document.IrefTable.MaxObjectNumber + 1}");
            trailer.Append($"/Root {_document.Catalog.ObjectNumber} 0 R");

            var info = _document.Trailer.Elements.GetReference(PdfTrailer.Keys.Info);
            if (info != null)
                trailer.Append($"/Info {info.ObjectNumber} 0 R");

            if (prevOffset > 0)
                trailer.Append($"/Prev {prevOffset}");

            // Document ID
            var id = _document.Trailer.Elements[PdfTrailer.Keys.ID];
            if (id is PdfArray idArray && idArray.Elements.Count >= 2)
            {
                trailer.Append("/ID[");
                foreach (var item in idArray.Elements)
                {
                    if (item is PdfString str)
                        trailer.Append(str.ToString());
                }
                trailer.Append("]");
            }

            trailer.Append(">>");
            writer.WriteRaw(trailer.ToString());
        }

        /// <summary>
        /// Writes the main xref table.
        /// </summary>
        void WriteMainXRef(PdfWriter writer)
        {
            writer.WriteRaw("xref\n");

            var allRefs = _objectSets.GetAllReferencesInOrder().Where(r => r != null).ToArray();
            int maxObjNum = allRefs.Max(r => r.ObjectNumber);

            writer.WriteRaw($"0 {maxObjNum + 1}\n");

            // Free object entry
            writer.WriteRaw("0000000000 65535 f \n");

            // Create a lookup for positions
            var positionLookup = allRefs.ToDictionary(r => r.ObjectNumber, r => r.Position);

            // Write entries
            for (int i = 1; i <= maxObjNum; i++)
            {
                if (positionLookup.TryGetValue(i, out var pos))
                {
                    writer.WriteRaw($"{pos:D10} 00000 n \n");
                }
                else
                {
                    writer.WriteRaw("0000000000 00000 f \n");
                }
            }
        }

        /// <summary>
        /// Writes the main trailer.
        /// </summary>
        void WriteMainTrailer(PdfWriter writer)
        {
            var trailer = new StringBuilder();
            trailer.Append("<<");
            trailer.Append($"/Size {_document.IrefTable.MaxObjectNumber + 1}");
            trailer.Append($"/Root {_document.Catalog.ObjectNumber} 0 R");

            var info = _document.Trailer.Elements.GetReference(PdfTrailer.Keys.Info);
            if (info != null)
                trailer.Append($"/Info {info.ObjectNumber} 0 R");

            // Document ID
            var id = _document.Trailer.Elements[PdfTrailer.Keys.ID];
            if (id is PdfArray idArray && idArray.Elements.Count >= 2)
            {
                trailer.Append("/ID[");
                foreach (var item in idArray.Elements)
                {
                    if (item is PdfString str)
                        trailer.Append(str.ToString());
                }
                trailer.Append("]");
            }

            trailer.Append(">>\n");
            writer.WriteRaw(trailer.ToString());
        }

        /// <summary>
        /// Gets the size of the PDF header.
        /// </summary>
        int GetHeaderSize()
        {
            // "%PDF-1.x\n%?????\n" = approximately 15 bytes
            // Plus any verbose mode comments
            return _writer.Layout == PdfWriterLayout.Verbose ? 500 : 15;
        }

        /// <summary>
        /// Estimates the size of the main xref section.
        /// </summary>
        int EstimateMainXRefSize()
        {
            int objectCount = _objectSets.TotalObjectCount;
            // "xref\n0 N\n" + (N+1 entries * 20 bytes) + trailer (~150 bytes) + "startxref\nN\n%%EOF\n" (~20 bytes)
            return 10 + ((objectCount + 1) * 20) + 200;
        }

        /// <summary>
        /// Calculates the number of bits needed to represent a value.
        /// </summary>
        static int BitsNeeded(int value)
        {
            if (value <= 0) return 1;
            int bits = 0;
            while (value > 0)
            {
                bits++;
                value >>= 1;
            }
            return Math.Max(bits, 1);
        }
    }
}

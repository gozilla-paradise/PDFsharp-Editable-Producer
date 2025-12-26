// PDFsharp - A .NET library for processing PDF
// See the LICENSE file in the solution root for more information.

using System.Collections;
using PdfSharp.Pdf.Advanced;

namespace PdfSharp.Pdf.Linearization
{
    /// <summary>
    /// Analyzes a PDF document and categorizes objects for linearized output.
    /// Uses transitive closure to determine object dependencies for each page.
    /// </summary>
    internal sealed class LinearizationObjectCollector
    {
        readonly PdfDocument _document;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearizationObjectCollector"/> class.
        /// </summary>
        public LinearizationObjectCollector(PdfDocument document)
        {
            _document = document;
        }

        /// <summary>
        /// Collects and categorizes all objects in the document for linearized output.
        /// </summary>
        /// <returns>A <see cref="LinearizedObjectSets"/> containing categorized object references.</returns>
        public LinearizedObjectSets Collect()
        {
            var result = new LinearizedObjectSets();
            var allRefs = _document.IrefTable.AllReferences;

            // Track which objects belong to which pages
            var objectToPages = new Dictionary<PdfObjectID, HashSet<int>>();
            var pageClosures = new List<HashSet<PdfReference>>();

            // Get closure for each page
            int pageCount = _document.Pages.Count;
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                var page = _document.Pages[pageIndex];
                var pageClosure = GetTransitiveClosure(page);
                pageClosures.Add(pageClosure);

                // Track which pages reference each object
                foreach (var iref in pageClosure)
                {
                    if (!objectToPages.TryGetValue(iref.ObjectID, out var pages))
                    {
                        pages = new HashSet<int>();
                        objectToPages[iref.ObjectID] = pages;
                    }
                    pages.Add(pageIndex);
                }
            }

            // Identify document-level objects (catalog, pages tree root, info)
            var documentLevelObjects = new List<PdfReference>();
            var catalogRef = _document.Catalog.Reference;
            if (catalogRef != null)
                documentLevelObjects.Add(catalogRef);

            // Pages tree root
            var pagesRef = _document.Catalog.Elements.GetReference(PdfCatalog.Keys.Pages);
            if (pagesRef != null && !documentLevelObjects.Contains(pagesRef))
                documentLevelObjects.Add(pagesRef);

            // Document info (optional)
            var infoRef = _document.Trailer.Elements.GetReference(PdfTrailer.Keys.Info);
            if (infoRef != null)
                documentLevelObjects.Add(infoRef);

            // Outlines/bookmarks if present
            var outlinesRef = _document.Catalog.Elements.GetReference(PdfCatalog.Keys.Outlines);
            if (outlinesRef != null)
                documentLevelObjects.Add(outlinesRef);

            result.DocumentLevelObjects = documentLevelObjects.ToArray();

            // Categorize objects
            var firstPageObjects = new List<PdfReference>();
            var sharedObjects = new List<PdfReference>();
            var remainingPageObjects = new List<PdfReference>[pageCount > 1 ? pageCount - 1 : 0];
            for (int i = 0; i < remainingPageObjects.Length; i++)
                remainingPageObjects[i] = new List<PdfReference>();

            // First, identify shared objects and first-page exclusive objects
            var documentLevelSet = new HashSet<PdfObjectID>(documentLevelObjects.Select(r => r.ObjectID));

            if (pageClosures.Count > 0)
            {
                foreach (var iref in pageClosures[0])
                {
                    // Skip document-level objects
                    if (documentLevelSet.Contains(iref.ObjectID))
                        continue;

                    if (objectToPages.TryGetValue(iref.ObjectID, out var pages) && pages.Count > 1)
                    {
                        // Object is used by multiple pages - shared
                        sharedObjects.Add(iref);
                    }
                    else
                    {
                        // Object is exclusive to first page
                        firstPageObjects.Add(iref);
                    }
                }
            }

            result.FirstPageObjects = firstPageObjects.ToArray();

            // Now categorize remaining pages' exclusive objects
            var processedObjects = new HashSet<PdfObjectID>();
            foreach (var iref in documentLevelObjects)
                processedObjects.Add(iref.ObjectID);
            foreach (var iref in firstPageObjects)
                processedObjects.Add(iref.ObjectID);
            foreach (var iref in sharedObjects)
                processedObjects.Add(iref.ObjectID);

            for (int pageIndex = 1; pageIndex < pageCount; pageIndex++)
            {
                var pageExclusiveObjects = new List<PdfReference>();
                foreach (var iref in pageClosures[pageIndex])
                {
                    if (processedObjects.Contains(iref.ObjectID))
                        continue;

                    if (objectToPages.TryGetValue(iref.ObjectID, out var pages) && pages.Count > 1)
                    {
                        // Shared object not yet added
                        if (!sharedObjects.Any(r => r.ObjectID == iref.ObjectID))
                        {
                            sharedObjects.Add(iref);
                            processedObjects.Add(iref.ObjectID);
                        }
                    }
                    else
                    {
                        // Exclusive to this page
                        pageExclusiveObjects.Add(iref);
                        processedObjects.Add(iref.ObjectID);
                    }
                }
                remainingPageObjects[pageIndex - 1] = pageExclusiveObjects;
            }

            result.RemainingPageObjects = remainingPageObjects.Select(l => l.ToArray()).ToArray();
            result.SharedObjects = sharedObjects.ToArray();

            return result;
        }

        /// <summary>
        /// Gets the transitive closure of all objects reachable from the specified object.
        /// </summary>
        HashSet<PdfReference> GetTransitiveClosure(PdfObject root)
        {
            var references = new HashSet<PdfReference>();
            var stack = new Stack<PdfObject>();

            // Add the root object's reference if it has one
            if (root.Reference != null)
                references.Add(root.Reference);

            if (root is PdfDictionary or PdfArray)
                stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                FindReferencedItems(current, references, stack);
            }

            return references;
        }

        /// <summary>
        /// Finds all referenced items in a dictionary or array and adds them to the collection.
        /// </summary>
        void FindReferencedItems(PdfObject pdfObj, HashSet<PdfReference> references, Stack<PdfObject> stack)
        {
            IEnumerable? items = null;

            if (pdfObj is PdfDictionary dict)
                items = dict.Elements.Values;
            else if (pdfObj is PdfArray array)
                items = array.Elements;
            else
                return;

            foreach (var item in items)
            {
                if (item is PdfReference iref)
                {
                    // Skip if already processed
                    if (references.Contains(iref))
                        continue;

                    // Skip if from different document
                    if (iref.Document != null && !ReferenceEquals(iref.Document, _document))
                        continue;

                    var value = iref.Value;
                    if (value != null && iref.ObjectID.ObjectNumber != 0)
                    {
                        references.Add(iref);

                        if (value is PdfDictionary or PdfArray)
                            stack.Push(value);
                    }
                }
                else if (item is PdfObject obj and (PdfDictionary or PdfArray))
                {
                    // Direct object - traverse it
                    stack.Push(obj);
                }
            }
        }
    }
}

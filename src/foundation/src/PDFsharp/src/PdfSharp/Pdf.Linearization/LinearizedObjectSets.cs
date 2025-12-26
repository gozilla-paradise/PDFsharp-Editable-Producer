// PDFsharp - A .NET library for processing PDF
// See the LICENSE file in the solution root for more information.

using PdfSharp.Pdf.Advanced;

namespace PdfSharp.Pdf.Linearization
{
    /// <summary>
    /// Contains the categorized object references for linearized PDF output.
    /// Objects are grouped by their role in the linearized structure.
    /// </summary>
    internal sealed class LinearizedObjectSets
    {
        /// <summary>
        /// Gets or sets the linearization dictionary reference.
        /// This must be object number 1 in the linearized file.
        /// </summary>
        public PdfReference? LinearizationDictionary { get; set; }

        /// <summary>
        /// Gets or sets document-level objects that appear at the beginning.
        /// Includes: Catalog, Pages tree root, document info.
        /// </summary>
        public PdfReference[] DocumentLevelObjects { get; set; } = [];

        /// <summary>
        /// Gets or sets objects required to display the first page.
        /// Includes: Page dictionary, resources, content streams, fonts, images.
        /// </summary>
        public PdfReference[] FirstPageObjects { get; set; } = [];

        /// <summary>
        /// Gets or sets the hint stream reference.
        /// </summary>
        public PdfReference? HintStream { get; set; }

        /// <summary>
        /// Gets or sets objects for each remaining page (index 0 = page 2, etc.).
        /// Each array contains objects exclusive to that page.
        /// </summary>
        public PdfReference[][] RemainingPageObjects { get; set; } = [];

        /// <summary>
        /// Gets or sets objects shared between multiple pages.
        /// These appear after all page-specific objects.
        /// </summary>
        public PdfReference[] SharedObjects { get; set; } = [];

        /// <summary>
        /// Gets the total count of all objects.
        /// </summary>
        public int TotalObjectCount
        {
            get
            {
                int count = DocumentLevelObjects.Length + FirstPageObjects.Length + SharedObjects.Length;
                if (LinearizationDictionary != null) count++;
                if (HintStream != null) count++;
                foreach (var pageObjects in RemainingPageObjects)
                    count += pageObjects.Length;
                return count;
            }
        }

        /// <summary>
        /// Returns all object references in linearized order.
        /// </summary>
        public IEnumerable<PdfReference> GetAllReferencesInOrder()
        {
            // 1. Linearization dictionary (must be first)
            if (LinearizationDictionary != null)
                yield return LinearizationDictionary;

            // 2. Document-level objects (catalog, pages root)
            foreach (var iref in DocumentLevelObjects)
                yield return iref;

            // 3. First page objects
            foreach (var iref in FirstPageObjects)
                yield return iref;

            // 4. Hint stream
            if (HintStream != null)
                yield return HintStream;

            // 5. Remaining pages
            foreach (var pageObjects in RemainingPageObjects)
            {
                foreach (var iref in pageObjects)
                    yield return iref;
            }

            // 6. Shared objects
            foreach (var iref in SharedObjects)
                yield return iref;
        }
    }
}

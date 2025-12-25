// PDFsharp - A .NET library for processing PDF
// See the LICENSE file in the solution root for more information.

using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member because it is for internal use only.

namespace PdfSharp.Logging
{
    /// <summary>
    /// Defines the logging event IDs of PDFsharp.
    /// </summary>
    public static class PdfSharpEventId  // draft...
    {
        public const int DocumentCreated = StartId + 1;
        public const int DocumentSaved = StartId + 2;
        public const int PageCreated = StartId + 3;
        public const int PageAdded = StartId + 4;
        public const int GraphicsCreated = StartId + 5;
        public const int FontCreated = StartId + 6;

        // Reading PDFs
        public const int PdfReaderIssue = StartId + 10;
        public const int StreamIssue = StartId + 11;
        public const int EndOfStreamReached = StartId + 12;
        public const int SkippedIllegalBlanksAfterStreamKeyword = StartId + 13;
        public const int StreamKeywordFollowedBySingleCR = StartId + 14;
        public const int StreamKeywordFollowedByIllegalBytes = StartId + 15;

        internal const int Placeholder = StartId + 1234;
        const int StartId = 50000;
    };

    public static class PdfSharpEventName
    {
        public const string DocumentCreated = "Document created";
        public const string DocumentSaved = "Document saved";
        public const string PageCreated = "Page created";
        public const string PageAdded = "Page creation2";
        public const string GraphicsCreated = "Graphics created";
        public const string FontCreated = "Font created";

        public const string PdfReaderIssue = "PDF reader issue";
        public const string StreamIssue = "Stream issue";
        public const string EndOfStreamReached = "End of stream reached";
        public const string SkippedIllegalBlanksAfterStreamKeyword = "Skipped illegal blanks after stream keyword";
        public const string StreamKeywordFollowedBySingleCR = "Stream keyword followed by single CR";
        public const string StreamKeywordFollowedByIllegalBytes = "Stream keyword followed by illegal bytes";
    }

    public static class PdfSharpEvent
    {
        public static EventId DocumentCreate = new(PdfSharpEventId.DocumentCreated, PdfSharpEventName.DocumentCreated);
        public static EventId DocumentSaved = new(PdfSharpEventId.DocumentSaved, PdfSharpEventName.DocumentSaved);
        public static EventId PageCreate = new(PdfSharpEventId.PageCreated, PdfSharpEventName.PageCreated);
        public static EventId PageAdded = new(PdfSharpEventId.PageAdded, PdfSharpEventName.PageAdded);
        public static EventId FontCreate = new(PdfSharpEventId.FontCreated, PdfSharpEventName.FontCreated);

        public static EventId PdfReaderIssue = new(PdfSharpEventId.PdfReaderIssue, PdfSharpEventName.PdfReaderIssue);

        public static EventId Placeholder = new(999999, "Placeholder");
    }

    /// <summary>
    /// Defines the logging high performance messages of PDFsharp.
    /// </summary>
    public static class LogMessages
    {
        public static void PdfDocumentCreated(this ILogger logger, string? documentName)
        {
            logger.Log(LogLevel.Information, new EventId(PdfSharpEventId.DocumentCreated, PdfSharpEventName.DocumentCreated),
                "New PDF document '{DocumentName}' created.", documentName);
        }

        public static void PdfDocumentSaved(this ILogger logger, string? documentName)
        {
            logger.Log(LogLevel.Information, new EventId(PdfSharpEventId.DocumentSaved, PdfSharpEventName.DocumentSaved),
                "PDF document '{DocumentName}' saved.", documentName);
        }

        public static void NewPdfPageCreated(this ILogger logger, string? documentName)
        {
            logger.Log(LogLevel.Information, new EventId(PdfSharpEventId.PageCreated, PdfSharpEventName.PageCreated),
                "New PDF page added to document '{DocumentName}'.", documentName);
        }

        public static void ExistingPdfPageAdded(this ILogger logger, string? documentName)
        {
            logger.Log(LogLevel.Information, new EventId(PdfSharpEventId.PageAdded, PdfSharpEventName.PageAdded),
                "Existing PDF page added to document '{DocumentName}'.", documentName);
        }

        public static void XGraphicsCreated(this ILogger logger, string? source)
        {
            logger.Log(LogLevel.Information, new EventId(PdfSharpEventId.GraphicsCreated, PdfSharpEventName.GraphicsCreated),
                "New XGraphics created from '{Source}'.", source);
        }

        // Reading PDFs

        public static void StreamIssue(this ILogger logger, string status, int bytesRead, int length)
        {
            logger.Log(LogLevel.Error, new EventId(PdfSharpEventId.StreamIssue, PdfSharpEventName.StreamIssue),
                "{Status} {BytesRead} of {Length} bytes were received. " +
                "We strongly recommend using streams with PdfReader whose content is fully available. " +
                "Copy the stream containing the file to a MemoryStream for example.",
                status, bytesRead, length);
        }

        public static void EndOfStreamReached(this ILogger logger, int length, SizeType position, int bytesRead)
        {
            logger.Log(LogLevel.Warning, new EventId(PdfSharpEventId.EndOfStreamReached, PdfSharpEventName.EndOfStreamReached),
                "End of stream reached while reading {Length} bytes at position {Position}, but got only {BytesRead} bytes.",
                length, position, bytesRead);
        }

        public static void SkippedIllegalBlanksAfterStreamKeyword(this ILogger logger, int blankCount, SizeType position, PdfObjectID objectId)
        {
            logger.Log(LogLevel.Warning, new EventId(PdfSharpEventId.SkippedIllegalBlanksAfterStreamKeyword, PdfSharpEventName.SkippedIllegalBlanksAfterStreamKeyword),
                "Skipped {BlankCount} illegal blanks behind keyword 'stream' at position {Position} in object {ObjectId}.",
                blankCount, position, objectId);
        }

        public static void StreamKeywordFollowedBySingleCR(this ILogger logger, SizeType position, PdfObjectID objectId)
        {
            logger.Log(LogLevel.Warning, new EventId(PdfSharpEventId.StreamKeywordFollowedBySingleCR, PdfSharpEventName.StreamKeywordFollowedBySingleCR),
                "Keyword 'stream' followed by single CR is illegal at position {Position} in object {ObjectId}.",
                position, objectId);
        }

        public static void StreamKeywordFollowedByIllegalBytes(this ILogger logger, SizeType position, PdfObjectID objectId)
        {
            logger.Log(LogLevel.Warning, new EventId(PdfSharpEventId.StreamKeywordFollowedByIllegalBytes, PdfSharpEventName.StreamKeywordFollowedByIllegalBytes),
                "Keyword 'stream' followed by illegal bytes at position {Position} in object {ObjectId}.",
                position, objectId);
        }
    }

#if true_
    class LogTestCode
    {
        void FooBar()
        {
            //var ss = PSEventId.Test1;
            //PdfSharpLogHost.Logger.LogError(ss, "message");
            LoggerMessage.Define<char>(LogLevel.Critical, )
        }
    }
#endif
}

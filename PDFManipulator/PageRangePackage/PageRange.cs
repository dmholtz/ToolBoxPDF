using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;

namespace PDFManipulator
{
    /// <summary>
    /// Represents a set of pages, which are identified by their pagenumber
    /// </summary>
    public class PageRange : SortedSet<int>
    {
        public PdfDocument Document { get; }

        public PageRange(PdfDocument referredDoc) : base()
        {
            Document = referredDoc ?? throw new ArgumentException("Referred document must exist and must not be null");
        }

        public new bool Add(int pageNumber)
        {
            if (pageNumber <= 0 || pageNumber > Document.GetNumberOfPages())
            {
                throw new PageNumberOutOfRangeException(pageNumber);
            }
            return base.Add(pageNumber);
        }

        public int GetNumberOfPages()
        {
            return this.Count;
        }

        public PageRange Invert()
        {
            throw new NotImplementedException();
        }

        public string GetPattern()
        {
            throw new NotImplementedException();
        }

        public static PageRange EntireDocument(PdfDocument referredDoc)
        {
            PageRange entireRange = new PageRange(referredDoc);
            for (int i = 1; i <= referredDoc.GetNumberOfPages(); i++)
            {
                entireRange.Add(i);
            }
            return entireRange;
        }

        public static PageRange FromPattern(PdfDocument referredDoc, string pattern)
        {
            PageRange pageRange = new PageRange(referredDoc);
            RangePattern rangePattern = new RangePattern(pattern);   // syntax check
            foreach (int page in rangePattern)
            {
                pageRange.Add(page);    // semantic check while adding
            }
            return pageRange;
        }
    }
}

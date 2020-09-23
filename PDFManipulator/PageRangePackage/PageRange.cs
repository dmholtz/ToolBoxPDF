using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.Text;

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

        /// <summary>
        /// Add a valid page number to this PageRange instance.
        /// The (unchecked) inherited Add-method is hidden.
        /// </summary>
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

        /// <summary>
        /// Inverts this instances page selection: Returns a new PageRange instance with all the pages 
        /// which this instance does not contain.
        /// </summary>
        public PageRange Invert()
        {
            PageRange inverted = new PageRange(Document);
            for (int i = 1; i <= Document.GetNumberOfPages(); i++)
            {
                if (!this.Contains(i))
                {
                    inverted.Add(i);
                }
            }
            return inverted;
        }

        /// <summary>
        /// Textually represents this instance as a string in RangePattern format. The textual representation is as simple as possible.
        /// Invariant: this.Equals(PageRange.FromPattern(this.Document, this.GetPattern)) = true;
        /// </summary>
        public string GetPattern()
        {
            StringBuilder pattern = new StringBuilder();
            PatternBuiltState builtState = PatternBuiltState.FIRST;
            for (int page = 1; page <= Document.GetNumberOfPages(); page++)
            {
                if (Contains(page))
                {
                    switch (builtState)
                    {
                        case PatternBuiltState.FIRST:
                            pattern.Append(page);
                            builtState = PatternBuiltState.ONE_PREDECESSOR;
                            break;
                        case PatternBuiltState.NO_PREDECESSOR:
                            pattern.Append(',');
                            pattern.Append(page);
                            builtState = PatternBuiltState.ONE_PREDECESSOR;
                            break;
                        case PatternBuiltState.ONE_PREDECESSOR:
                            builtState = PatternBuiltState.MORE_PREDECESSORS;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (builtState)
                    {
                        case PatternBuiltState.ONE_PREDECESSOR:
                            builtState = PatternBuiltState.NO_PREDECESSOR;
                            break;
                        case PatternBuiltState.MORE_PREDECESSORS:
                            pattern.Append('-');
                            pattern.Append(page - 1);
                            builtState = PatternBuiltState.NO_PREDECESSOR;
                            break;
                        default:
                            break;
                    }
                    
                }
            }
            if(builtState == PatternBuiltState.MORE_PREDECESSORS)
            {
                pattern.Append('-');
                pattern.Append(Document.GetNumberOfPages());
            }
            return pattern.ToString();
        }

        public override string ToString()
        {
            return GetPattern();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != this.GetType())
            {
                return false;
            }
            PageRange other = (PageRange)obj;
            if (Document != other.Document)
            {
                return false;
            }
            if (other.IsSubsetOf(this) && this.IsSubsetOf(other))
            {
                return true;
            }
            else
            {
                return false;
            }
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

        /// <summary>
        /// Flags the state of the GetPattern() method
        /// </summary>
        private enum PatternBuiltState
        {
            FIRST, // waiting for the first page number to be inserted
            NO_PREDECESSOR, // waiting for the next page number to be inserted, requires comma
            ONE_PREDECESSOR,    // waiting whether the following page is also inserted
            MORE_PREDECESSORS   // waiting for the series of contiguous pages to end
        }
    }
}

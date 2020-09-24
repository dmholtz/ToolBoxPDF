using System;

namespace PdfManipulator.PageRangePackage
{
    /// <summary>
    /// This exception is thrown whenever a page number is negative, zero or if it exceeds the number of pages of the corresponding document.
    /// </summary>
    public sealed class PageNumberOutOfRangeException : ArgumentException
    {
        public PageNumberOutOfRangeException(int invalidPageNumber) : base("The document does not contain page number " + invalidPageNumber.ToString()) { }
    }
}

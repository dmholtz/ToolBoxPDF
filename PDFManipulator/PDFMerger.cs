using System;
using System.Collections.Generic;
using System.IO;
using iText.Kernel.Pdf;

namespace PDFManipulator
{
    /// <summary>
    /// 
    /// </summary>
    class PDFMerger
    {

        private List<Stream> sources;



        /// <summary>
        /// Merges all associated PDF documents into one document. Result is pushed into the output stream.
        /// </summary>
        /// <param name="outputStream"></param>
        public void mergeInto(Stream outputStream)
        {
            if (outputStream == null || !outputStream.CanWrite)
            {
                throw new ArgumentException("Cannot access the output stream");
            }

            PdfWriter outputWriter = new PdfWriter(outputStream);
            PdfDocument outputDoc = new PdfDocument(outputWriter);

            foreach (var inputStream in sources)
            {
                PdfReader inputReader = new PdfReader(inputStream);
                PdfDocument sourceDoc = new PdfDocument(inputReader);
                for(int i = 1; i < sourceDoc.GetNumberOfPages(); i++)
                {
                    PdfPage p = sourceDoc.GetPage(i).CopyTo(outputDoc);
                    outputDoc.AddPage(p);
                }
                sourceDoc.Close();
                inputReader.Close();
            }

            outputDoc.Close();
            outputWriter.Close();
        }
    }
}

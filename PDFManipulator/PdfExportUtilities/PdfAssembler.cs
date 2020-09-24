using iText.Kernel.Pdf;
using PdfManipulator.PageRangePackage;
using System;
using System.Collections.Generic;
using System.IO;

namespace PdfManipulator.PdfExportUtilities
{
    class PdfAssembler
    {
        private Queue<ExportTask> exportTasks;
        private Stream outputStream;

        public PdfAssembler(Stream outputStream)
        {
            if (outputStream == null || !outputStream.CanWrite)
            {
                throw new ArgumentException("Cannot access the output stream");
            }
            this.outputStream = outputStream;
        }

        public void AppendTask(ExportTask t)
        {
            exportTasks.Enqueue(t);
        }

        public void ExportFile()
        {
            PdfWriter outputWriter = new PdfWriter(outputStream);
            PdfDocument outputDocument = new PdfDocument(outputWriter);

            AssembleFile(outputDocument);

            outputDocument.Close();
            outputWriter.Close();
        }

        private void AssembleFile(PdfDocument doc)
        {
            while(exportTasks.Count > 0)
            {
                ExportTask task = exportTasks.Dequeue();
                PageRange pageRange = task.Pages;
                foreach(var pageNum in pageRange)
                {
                    PdfPage page = pageRange.Document.GetPage(pageNum).CopyTo(doc);
                    
                    // execute transformation pipeline
                    foreach(var transform in task.TransformPipeline)
                    {
                        transform.Transform(page);
                    }

                    doc.AddPage(page);
                }
            }
        }
    }
}

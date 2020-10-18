using iText.Kernel.Pdf;
using ToolBoxPDF.Core.PageRangePackage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using iText.Kernel.Geom;
using System.Threading.Tasks;

namespace ToolBoxPDF.Core.IO
{
    public class PdfAssembler
    {
        private readonly Queue<ExportTask> exportTasks;
        private readonly Stream outputStream;

        public PdfAssembler(Stream outputStream)
        {
            if (outputStream == null || !outputStream.CanWrite)
            {
                throw new ArgumentException("Cannot access the output stream");
            }
            this.outputStream = outputStream;
            exportTasks = new Queue<ExportTask>();
        }

        public void AppendTask(ExportTask t)
        {
            exportTasks.Enqueue(t);
        }

        public void ExportFile()
        {          
            PdfWriter outputWriter = new PdfWriter(outputStream);
            ExportOperation(outputWriter);         
        }

        /// <summary>
        /// Encrypts a file with AES-256 using the given owner password. The user password is equal to the owner password. Owner and user are granted all permission.
        /// </summary>
        /// <param name="ownerPassword"></param>
        public void ExportFileEncrypted(string ownerPassword)
        {
            int permissions = 4096 - 3; // all permissions, @see EncryptionConstants
            byte[] rawOwnerPassword = new System.Text.ASCIIEncoding().GetBytes(ownerPassword);
            WriterProperties writerProperties = new WriterProperties();
            writerProperties.SetStandardEncryption(rawOwnerPassword, rawOwnerPassword, permissions, EncryptionConstants.ENCRYPTION_AES_256);
            PdfWriter outputWriter = new PdfWriter(outputStream, writerProperties);
            ExportOperation(outputWriter);
        }

        private void ExportOperation(PdfWriter outputWriter)
        {
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

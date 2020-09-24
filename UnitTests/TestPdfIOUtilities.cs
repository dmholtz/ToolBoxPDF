using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using NUnit.Framework;

using PdfManipulator.PageRangePackage;
using PdfManipulator.PdfIOUtilities;
using System.Collections.Generic;
using System.IO;

namespace UnitTests
{
    class PdfAssemblerTester
    {
        private void CreateTestFile(int numberOfPages)
        {
            PdfWriter writer = new PdfWriter("..\\..\\..\\TestAssets\\ResourceFile" + numberOfPages.ToString() + ".pdf");
            PdfDocument pdfdoc = new PdfDocument(writer);
            Document doc = new Document(pdfdoc);
            pdfdoc.SetDefaultPageSize(PageSize.A4);

            for (int i = 1; i <= numberOfPages; i++)
            {
                Paragraph header = new Paragraph("PAGE " + i.ToString()).SetTextAlignment(TextAlignment.CENTER).SetFontSize(20);
                Paragraph subheader = new Paragraph("Document Nr. " + numberOfPages.ToString()).SetTextAlignment(TextAlignment.CENTER).SetFontSize(15);
                doc.Add(header);
                doc.Add(subheader);
                if (i != numberOfPages)
                {
                    doc.Add(new AreaBreak());
                }
            }

            pdfdoc.Close();
        }

        private PdfDocument LoadTestFile(string filename)
        {
            PdfReader reader = new PdfReader(filename);
            return new PdfDocument(reader);
        }

        [OneTimeSetUp]
        public void PrepareTests()
        {
            CreateTestFile(4);
            CreateTestFile(5);
            CreateTestFile(6);
        }

        [TestCase(new string[] { "..\\..\\..\\TestAssets\\ResourceFile4.pdf", "..\\..\\..\\TestAssets\\ResourceFile5.pdf", "..\\..\\..\\TestAssets\\ResourceFile6.pdf" },
            "..\\..\\..\\TestAssets\\AssembledFile15.pdf", 15)]
        [TestCase(new string[] { "..\\..\\..\\TestAssets\\ResourceFile6.pdf", "..\\..\\..\\TestAssets\\ResourceFile4.pdf" },
            "..\\..\\..\\TestAssets\\AssembledFile10.pdf", 10)]
        public void TestMergeEntireFiles(string[] inputFileNames, string outputFilename, int expectedNumberOfPages)
        {
            List<PdfDocument> inputFiles = new List<PdfDocument>();
            foreach(var filename in inputFileNames)
            {
                inputFiles.Add(LoadTestFile(filename));
            }
            List<PageRange> relevantPages = new List<PageRange>();
            foreach(var doc in inputFiles)
            {
                relevantPages.Add(PageRange.EntireDocument(doc));
            }
            List<ExportTask> exportTasks = new List<ExportTask>();
            foreach(var pageRange in relevantPages)
            {
                exportTasks.Add(new ExportTask(pageRange /*no transform*/));
            }
            Stream outputStream = new FileStream(outputFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            PdfAssembler assembler = new PdfAssembler(outputStream);

            foreach(var task in exportTasks)
            {
                assembler.AppendTask(task);
            }

            assembler.ExportFile();

            PdfDocument result = LoadTestFile(outputFilename);

            Assert.AreEqual(expectedNumberOfPages, result.GetNumberOfPages());
        }

        [TestCase("..\\..\\..\\TestAssets\\ResourceFile5.pdf", "..\\..\\..\\TestAssets\\RotatedFile6.pdf", 6)]
        public void TestRotatePages(string inputFileName, string outputFilename, int expectedNumberOfPages)
        {
            PdfDocument inputFile = LoadTestFile(inputFileName);
            List<PageRange> relevantPages = new List<PageRange>();

            relevantPages.Add(PageRange.FromPattern(inputFile, "1,5"));
            relevantPages.Add(PageRange.FromPattern(inputFile, "3-4"));
            relevantPages.Add(PageRange.FromPattern(inputFile, "2-3"));
            relevantPages.Add(PageRange.FromPattern(inputFile, ""));

            List<ExportTask> exportTasks = new List<ExportTask>();

            exportTasks.Add(new ExportTask(relevantPages[0], new List<IPageTransformation>() { new PageRotation(PageOrientation.RotateLeft) }));
            exportTasks.Add(new ExportTask(relevantPages[1], new List<IPageTransformation>() { new PageRotation(PageOrientation.UpsideDown) }));
            exportTasks.Add(new ExportTask(relevantPages[2], new PageRotation(PageOrientation.RotateRight)));
            exportTasks.Add(new ExportTask(relevantPages[3], new PageRotation(PageOrientation.NoRotation)));

            Stream outputStream = new FileStream(outputFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            PdfAssembler assembler = new PdfAssembler(outputStream);

            foreach (var task in exportTasks)
            {
                assembler.AppendTask(task);
            }

            assembler.ExportFile();

            PdfDocument result = LoadTestFile(outputFilename);

            Assert.AreEqual(expectedNumberOfPages, result.GetNumberOfPages());
        }
    }
}

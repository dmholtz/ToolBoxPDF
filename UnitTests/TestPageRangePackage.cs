using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using NUnit.Framework;

using PdfManipulator.PageRangePackage;
using System.Collections.Generic;

namespace UnitTests
{
    public class PatternTester
    {
        [TestCase("", true)]
        [TestCase("0123", true)]
        [TestCase("0123,23", true)]
        [TestCase("1-7", true)]
        [TestCase("1-7,3,2-3,6-5,4,4,4-3,4-1098", true)]
        [TestCase("1-7,", false)]
        [TestCase("0234,", false)]
        [TestCase("1-", false)]
        public void SyntaxCheck(string pattern, bool expected)
        {
            bool actual = RangePattern.SyntaxCheck(pattern);
            Assert.AreEqual(expected, actual);
        }

        [TestCase("", new int[] { })]
        [TestCase("0-3", new int[] { 0, 1, 2, 3, })]
        [TestCase("11,12", new int[] { 11, 12 })]
        [TestCase("101,102 , 103", new int[] { 101, 102, 103 })]
        [TestCase("1,2-0", new int[] { 1, 0, 1, 2 })]
        [TestCase("0-2,1-  3", new int[] { 0, 1, 2, 1, 2, 3 })]
        [TestCase("0,1,2,3,4-4", new int[] { 0, 1, 2, 3, 4 })]
        public void IteratorCheck(string pattern, int[] expected)
        {
            List<int> actual = new List<int>();
            RangePattern rangePattern = new RangePattern(pattern);
            foreach (int page in rangePattern)
            {
                actual.Add(page);
            }

            Assert.AreEqual(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
    }

    public class PageRangeTester
    {
        private void CreateTestFile(int numberOfPages)
        {
            PdfWriter writer = new PdfWriter("..\\..\\..\\TestAssets\\TestFile"+numberOfPages.ToString()+".pdf");
            PdfDocument pdfdoc = new PdfDocument(writer);
            Document doc = new Document(pdfdoc);

            for (int i = 1; i <= numberOfPages; i++)
            {
                Paragraph header = new Paragraph("PAGE " + i.ToString()).SetTextAlignment(TextAlignment.CENTER).SetFontSize(20);
                doc.Add(header);
                if(i!=numberOfPages)
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

        private PdfDocument doc10;
        private PdfDocument doc100;   

        [OneTimeSetUp]
        public void PrepareTests()
        {
            CreateTestFile(10);
            CreateTestFile(100);
            doc10 = LoadTestFile("..\\..\\..\\TestAssets\\TestFile10.pdf");
            doc100 = LoadTestFile("..\\..\\..\\TestAssets\\TestFile100.pdf");
        }

        [TestCase("1-5,13-1,93-87", "1-13,87-93")]
        [TestCase("1", "1")]
        [TestCase("100-1", "1-100")]
        public void TestPageRangeFromPattern(string pattern, string expectedMinimalPattern)
        {
            PageRange actualpageRange = PageRange.FromPattern(doc100, pattern);
            PageRange expectedPageRange = PageRange.FromPattern(doc100, expectedMinimalPattern);
            string acutalMinimalPattern = actualpageRange.GetPattern();
            Assert.AreEqual(expectedMinimalPattern, acutalMinimalPattern);
            Assert.IsTrue(actualpageRange.Equals(expectedPageRange));
        }

        [TestCase("1-100")]
        public void TestEntireDocument(string expectedMinimalPattern)
        {
            PageRange pageRange = PageRange.EntireDocument(doc100);
            string acutalMinimalPattern = pageRange.GetPattern();
            Assert.AreEqual(expectedMinimalPattern, acutalMinimalPattern);
        }

        [TestCase("1-10", "")]
        [TestCase("", "1-10")]
        [TestCase("5-2,9", "1,6-8,10")]
        public void TestInvertPageRange(string nonInvertedPattern, string expectedInvertedPattern)
        {
            PageRange pageRange = PageRange.FromPattern(doc10, nonInvertedPattern);
            PageRange expectedInvertedPageRange = PageRange.FromPattern(doc10, expectedInvertedPattern);
            PageRange actualInvertedPageRange = pageRange.Invert();
            Assert.AreEqual(expectedInvertedPattern, actualInvertedPageRange.GetPattern());
            Assert.IsTrue(actualInvertedPageRange.Equals(expectedInvertedPageRange));
        }
    }
}
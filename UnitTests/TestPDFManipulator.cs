using NUnit.Framework;

using PDFManipulator;
using System;
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
            bool actual = RangePattern.syntaxCheck(pattern);
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
}
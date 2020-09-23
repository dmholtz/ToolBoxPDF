using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PDFManipulator
{
    public class RangePattern : IEnumerable<int>
    {
        private readonly string pattern;

        public RangePattern(string pattern)
        {
            pattern = String.Concat(pattern.Where(c => !Char.IsWhiteSpace(c)));
            if (!syntacticallyValid(pattern))
            {
                throw new ArgumentException("Syntactically invalid page range pattern");
            }
            this.pattern = pattern;
        }

        public static bool syntacticallyValid(string pattern)
        {
            Regex syntax = new Regex(@"^(((\d+)|(\d+[-]\d+))([,]((\d+)|(\d+[-]\d+)))*)?$");
            return syntax.IsMatch(pattern);
        }

        public IEnumerator<int> GetEnumerator()
        {
            Regex numberRegex = new Regex(@"\d");
            Match numberMatch = numberRegex.Match(pattern);
            int streamIndex = 0;

            while (streamIndex < pattern.Length)
            {
                int pageNumber = Int32.Parse(numberMatch.Value);
                streamIndex += numberMatch.Length;

                numberMatch = numberMatch.NextMatch();
                if (pattern[streamIndex++] == ',')
                {  
                    // case x,y
                    yield return pageNumber;
                }
                else
                {
                    // case x-y
                    int otherPageNumber = Int32.Parse(numberMatch.Value);
                    streamIndex += numberMatch.Length;

                    int lowerBound = pageNumber < otherPageNumber ? pageNumber : otherPageNumber;
                    int upperBound = pageNumber < otherPageNumber ? otherPageNumber : pageNumber;

                    for (int i = lowerBound; i <= upperBound; i++)
                    {
                        yield return i;
                    }
                    numberMatch = numberMatch.NextMatch();
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

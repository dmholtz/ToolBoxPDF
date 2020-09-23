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
            if (!syntaxCheck(pattern))
            {
                throw new ArgumentException("Syntactically invalid page range pattern");
            }
            this.pattern = pattern;
        }

        /// <summary>
        /// Returns true if and only if the given pattern is syntactically valid.
        /// The pattern must not contain any whitespaces.
        /// </summary>
        public static bool syntaxCheck(string pattern)
        {
            Regex syntax = new Regex(@"^(((\d+)|(\d+[-]\d+))([,]((\d+)|(\d+[-]\d+)))*)?$");
            return syntax.IsMatch(pattern);
        }

        public IEnumerator<int> GetEnumerator()
        {
            Regex numberRegex = new Regex(@"\d+");
            Match numberMatch = numberRegex.Match(pattern);

            while (numberMatch.Success)
            {
                // Loop invariant: There is at least one more number in the pattern
                int pageNumber = Int32.Parse(numberMatch.Value);

                numberMatch = numberMatch.NextMatch();

                if (numberMatch.Success && pattern[numberMatch.Index - 1] == '-')
                {
                    // case x-y
                    int otherPageNumber = Int32.Parse(numberMatch.Value);

                    int lowerBound = pageNumber < otherPageNumber ? pageNumber : otherPageNumber;
                    int upperBound = pageNumber < otherPageNumber ? otherPageNumber : pageNumber;

                    for (int i = lowerBound; i <= upperBound; i++)
                    {
                        yield return i;
                    }
                    numberMatch = numberMatch.NextMatch();
                }
                else
                {
                    // case x,y
                    yield return pageNumber;
                }
                // Loop invariant: numberMatch states, whether there is at least one more number in the pattern
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

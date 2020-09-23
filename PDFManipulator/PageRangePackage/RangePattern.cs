using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PDFManipulator
{
    /// <summary>
    /// This class represents a pattern by which a range of page numbers can be textually represented
    /// Valid patterns might be:
    /// 1,2,3,4,5
    /// 1-12
    /// 1-12,13,14-16
    /// </summary>
    public class RangePattern : IEnumerable<int>
    {
        private readonly string pattern;

        /// <summary>
        /// Removes whitespaces from the pattern string and saves the pattern in case the syntax check was successful.
        /// </summary>
        /// <param name="pattern"></param>
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
        /// @requires: The pattern must not contain any whitespaces.
        /// </summary>
        public static bool syntaxCheck(string pattern)
        {
            Regex syntax = new Regex(@"^(((\d+)|(\d+[-]\d+))([,]((\d+)|(\d+[-]\d+)))*)?$");
            return syntax.IsMatch(pattern);
        }

        /// <summary>
        /// Enumerates all page numbers of this pattern
        /// </summary>
        /// <returns></returns>
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitScc
{
    public class DiffTool
    {
        public bool HasChange(string text)
        {
            return text.StartsWith("+") || text.StartsWith("-");
        }

        public List<string> GetChanges(string[] diffLines, int startLine, int endLine)
        {
            var lines = new List<string>();

            int min = Math.Min(startLine, endLine);
            int max = Math.Max(startLine, endLine);
            if (min < 4) min = 4;
            if (max > diffLines.Length - 1) max = diffLines.Length - 1;
            startLine = min;
            endLine = max;

            // find start of the change
            string text = diffLines[min];
            while (min > 4)
            {
                if (text.StartsWith("@@"))
                {
                    break;
                }
                else text = diffLines[--min];
            }
            if (min < 4) min = 4;

            // find end of the change
            text = diffLines[max];
            while (max < diffLines.Length - 1)
            {
                if (text.StartsWith("@@"))
                {
                    max--;
                    break;
                }
                else text = diffLines[++max];
            }
            if (max > diffLines.Length - 1) max = diffLines.Length - 1;


            // add change scope
            for (int i = min; i < startLine; i++)
            {
                var line = diffLines[i];
                // ignore none selected changes
                if (i == min || !HasChange(line))
                {
                    lines.Add(line);
                }
            }

            // add changes
            for (int i = startLine; i <= endLine; i++)
            {
                var line = diffLines[i];
                lines.Add(line);
            }

            // add remaining scope
            for (int i = endLine + 1; i <= max; i++)
            {
                var line = diffLines[i];
                // ignore none selected changes
                if (!HasChange(line))
                {
                    lines.Add(line);
                }
            }
            return lines;
        }

        public List<DiffHunk> GetPatches(string[] diffLines, int startLine, int endLine)
        {
            int min = Math.Min(startLine, endLine);
            int max = Math.Max(startLine, endLine);
            if (min < 1) min = 1;
            if (max > diffLines.Length - 1) max = diffLines.Length - 1;
            startLine = min;
            endLine = max;

            var patches = new List<DiffHunk>();
            var hunks = Parse(diffLines);

            foreach(var hunk in hunks)
            {
                if (hunk.LastLineIndex < startLine) continue;
                if (hunk.FirstLineIndex > endLine) break;

                patches.Add(hunk);
            }
            return patches;
        }

        public List<DiffHunk> Parse(string[] diffLines)
        {
            const string HUNK_HEADER = "^@@ -([0-9,]+) \\+([0-9,]+) @@(.*)";

            DiffHunk hunk = null;
            var hunks = new List<DiffHunk>();
            for(int idx=0; idx<diffLines.Length; idx++)
            {
                var line = diffLines[idx];
                var match = Regex.Match(line, HUNK_HEADER);

                if(match.Success)
                {
                    hunk = new DiffHunk {
                        FirstLineIndex = idx,
                        Heading = match.Groups[3].Value,
                        Lines = new List<string> { line },
                        OldBlock = ParseRange(match.Groups[1].Value),
                        NewBlock = ParseRange(match.Groups[2].Value),
                    };
                    hunks.Add(hunk);
                }
                else if(hunk != null)
                {
                    hunk.Lines.Add(line);
                }
            }
            return hunks;
        }

        private int[] ParseRange(string text)
        {
            var ss = text.Split(',');
            return ss.Select(s => { return int.Parse(s); }).ToArray();

        }
    }
}

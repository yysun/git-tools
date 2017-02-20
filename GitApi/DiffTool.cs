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

        // https://github.com/git-cola/git-cola/blob/d928ee9bcecc4fb56cc214ce135b27144d854940/cola/diffparse.py
        public List<DiffHunk> GetHunks(string[] diffLines, int startLine, int endLine)
        {
            int min = Math.Min(startLine, endLine);
            int max = Math.Max(startLine, endLine);
            if (min < 1) min = 1;
            if (max > diffLines.Length) max = diffLines.Length;
            startLine = min - 1;
            endLine = max - 1;

            var patches = new List<DiffHunk>();
            var hunks = Parse(diffLines);

            foreach(var hunk in hunks)
            {
                if (hunk.LastLineIndex < startLine) continue;
                if (hunk.FirstLineIndex > endLine) break;

                var lines = new List<string>();
                var skipped = false;
                var counts = new Dictionary<char, int> {
                    { ' ', 0 },
                    { '+', 0 },
                    { '-', 0 },
                    { '\\', 0 },
                } ;
                for(var i=0; i<hunk.Lines.Count(); i++)
                {
                    var line = hunk.Lines[i];
                    if (line.Length == 0) continue;

                    var lineIdx = hunk.FirstLineIndex + i;
                    var type = line[0];

                    if (lineIdx < startLine || lineIdx > endLine)
                    {
                        if (type == '+')
                        {
                            skipped = true;
                            hunk.NewBlock[1] -= 1;
                            continue;
                        }
                        else if (type == '-')
                        {
                            type = ' ';
                            line = type + line.Substring(1);
                            hunk.NewBlock[1] += 1;
                        }
                    }
                    if (type == '\\' && skipped) continue;
                    lines.Add(line);
                    counts[type] += 1;
                    skipped = false;
                }

                if (counts['+'] <= 0 && counts['-'] <= 0) continue;
                hunk.Lines = lines;
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
                        FirstLineIndex = idx + 1,
                        Heading = match.Groups[3].Value,
                        Lines = new List<string>(),
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

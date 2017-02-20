using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
    }
}

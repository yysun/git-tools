using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitScc
{
    public class DiffHunk
    {
        public string Heading { get; set; }
        public List<string> Lines { get; set; }
        public int[] OldBlock { get; set; }
        public int[] NewBlock { get; set; }
        public int FirstLineIndex { get; set; }
        public int LastLineIndex
        {
            get { return this.FirstLineIndex + this.Lines.Count() - 1; }
        }

    }
}

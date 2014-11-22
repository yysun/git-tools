using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Gitscc
{
    public abstract class Log
    {
        private static string logFileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "gitscc.log");

        public static void WriteLine(string format, params object[] objects)
        {
            //#if(DEBUG)
            var msg = string.Format(format, objects);
            msg = string.Format("{0} {1}\r\n\r\n", DateTime.UtcNow.ToString(), msg);
            File.AppendAllText(logFileName, msg);
            //#endif
        }
    }
}

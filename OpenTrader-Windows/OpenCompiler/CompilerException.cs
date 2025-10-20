using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCompiler
{
    internal class CompilerException : Exception
    {

        internal int LineNumber { get; set; }
        internal int ColumnNumber { get; set; }

        internal CompilerException(string message, int lineNumber, int columnNumber) : base(message)
        {
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }
    }
}

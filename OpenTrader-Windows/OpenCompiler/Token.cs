using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCompiler
{
    internal class Token
    {
        internal TokenGroup Group;
        internal string Source="";
        private int line;
        private int column;

        internal int Line
        {
            get { return line; }
        }

        internal int Column
        {
            get { return column - Source.Length; }
        }

        internal Token(int line, int column)
        {
            Group = TokenGroup.none;
            Source = "";
            this.line = line;
            this.column = column;
        }   
    }
}

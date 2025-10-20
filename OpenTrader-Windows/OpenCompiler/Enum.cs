using OpenCompiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCompiler
{
    internal class Enum
    {
        internal string Name;
        internal List<(string name, int constant)> Values  = new();
        internal Symbol Symbol;

        internal Enum(string name, Symbol symbol)
        {
            Name = name;
            Symbol = symbol;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCompiler
{
    internal class Class
    {
        internal string Name { get; set;  }
        internal Class? SuperClass { get; }

        internal List<Symbol>? Symbols { get; set; }

       internal Class(string name)
        {
            Name = name;
            Symbols = new List<Symbol>();
        }

    }
}

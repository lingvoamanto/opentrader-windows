using OpenCompiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace OpenCompiler
{
    internal class Symbol
    {
        internal Block? Block;
        internal string Name;
        internal SymbolType Type;
        internal bool isStack = false;
        internal int Offset;

        internal Symbol(string name, SymbolType type) {
            Name = name;
            Type = type;
        }

        internal ArraySymbol ToArraySymbol()
        {
            var arraySymbol = new ArraySymbol(Name)
            {
                Block = Block,
                Type = Type,
                isStack = isStack,
                Offset = Offset
            };

            return arraySymbol;
        }
    }

    internal class ProcSymbol : Symbol
    {
        internal List<SymbolType> Parameters;
        internal bool Bridge;
        internal SymbolType ReturnType;
        internal int Address;

        internal ProcSymbol(string name, SymbolType returnType) : base(name, SymbolType.Proc)
        {
            Parameters = new List<SymbolType>();
            Bridge = false;
            ReturnType = returnType == null ? SymbolType.Void : returnType;
        }

        internal ProcSymbol(string name, SymbolType returnType, List<SymbolType> parameters, bool bridge = false) : base(name, SymbolType.Proc)
        {
            Parameters = parameters;
            Bridge = bridge;
            ReturnType = returnType == null ? SymbolType.Void : returnType;
        }
    }

    internal class ArraySymbol : Symbol
    {
        internal SymbolType ReturnType;

        internal ArraySymbol(string name, SymbolType returnType) : base(name, SymbolType.Array)
        {
            ReturnType = returnType == null ? SymbolType.Void : returnType;
        }

        internal ArraySymbol(string name) : base(name, SymbolType.Array)
        {
            ReturnType = SymbolType.Void;
        }
    }

    internal class EnumValue
    {
        internal string Name;
        internal int Constant;

        internal EnumValue(string name, int constant)
        {
            Name = name;
            Constant = constant;
        }
    }

    internal class EnumSymbol : Symbol
    {
        internal List<EnumValue> Values = new();

        internal EnumSymbol(string name) : base(name, SymbolType.Enum)
        {
            Values = new();
        }

        internal EnumSymbol(string name, List<EnumValue> values) : base(name, SymbolType.Enum)
        {
            Values = values;
        }
    }
}

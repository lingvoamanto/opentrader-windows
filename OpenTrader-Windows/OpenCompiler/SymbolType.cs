using OpenTrader.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCompiler
{
    internal class SymbolType
    {
        private readonly string name;
        internal SymbolType ReturnType { get; set; }

        internal string Name
        {
            get => name;
        }

        internal SymbolType(string name)
        {
            this.name = name;
        }

        internal const string StringSymbol = "string", SeriesSymbol = "series", IntSymbol = "int", PaneSymbol = "pane",
            FloatSymbol = "float", BoolSymbol = "bool", ArraySymbol = "array", ProcSymbol = "proc", CandlesSymbol = "Candles",
            PatternSymbol = "Pattern", ExtremaSymbol = "Extrema", CandleSymbol = "Candle";

        private static SymbolType? _series, _proc, _void, _pane, _enum, _int, _string, _bool, _float, _array, _pattern, _extrema, _candles, _candle;

        internal static SymbolType Series
        {
            get => _series == null ? (_series = new SymbolType(SeriesSymbol)) : _series;
        }

        internal static SymbolType Proc
        {
            get => _proc == null ? (_proc = new SymbolType("proc")) : _proc;
        }
        internal static SymbolType Void
        {
            get => _void == null ? (_void = new SymbolType("void")) : _void;
        }
        internal static SymbolType Pane
        {
            get => _pane == null ? (_pane = new SymbolType(PaneSymbol)) : _pane;
        }
        internal static SymbolType Enum
        {
            get => _enum == null ? (_enum = new SymbolType("enum")) : _enum;
        }
        internal static SymbolType Int
        {
            get => _int == null ? (_int = new SymbolType(IntSymbol)) : _int;
        }
        internal static SymbolType String
        {
            get => _string == null ? (_string = new SymbolType(StringSymbol)) : _string;
        }
        internal static SymbolType Bool
        {
            get => _bool == null ? (_bool = new SymbolType(BoolSymbol)) : _bool;
        }
        internal static SymbolType Float
        {
            get => _float == null ? (_float = new SymbolType(FloatSymbol)) : _float;
        }
        internal static SymbolType Array
        {
            get => new SymbolType(ArraySymbol);
        }
        internal static SymbolType Pattern
        {
            get => _pattern == null ? (_pattern = new SymbolType(PatternSymbol)) : _pattern;
        }
        internal static SymbolType Extrema
        {
            get => _extrema == null ? (_extrema = new SymbolType(ExtremaSymbol)) : _extrema;
        }
        internal static SymbolType Candles
        {
            get => _candles == null ? (_candles = new SymbolType(CandlesSymbol)) : _candles;
        }
        internal static SymbolType Candle
        {
            get => _candle == null ? (_candle = new SymbolType(CandleSymbol)) : _candle;
        }

        public static bool operator ==(SymbolType? a, SymbolType? b)
        {
            if (b is null)
            {
                return a is null;
            }
            else if (a.name == "proc" && b.name == "proc")
            {
                return a.ReturnType == b.ReturnType;
            }
            else if (a.name == "array" && b.name == "array")
            {
                return a.ReturnType == b.ReturnType;
            }
            else
            {
                return a.name == b.name;
            }
        }

        public static bool operator !=(SymbolType? a, SymbolType? b)
        {
            if (b == null)
            {
                return a != null;
            }
            else if (a.name == "proc" && b.name == "proc")
            {
                return a.ReturnType != b.ReturnType;
            }
            else if (a.name == "array" && b.name == "array")
            {
                return a.name != b.name;
            }
            else
            {
                return a.name != b.name;
            }
        }


    }

    internal class EnumType : SymbolType
    {
        EnumSymbol Symbol;
        internal EnumType(EnumSymbol enumSymbol) : base(enumSymbol.Name)
        {
            Symbol = enumSymbol;
        }
    }
}



using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using Svg;
using Microsoft.CodeAnalysis;
using OpenTrader.Indicators;
using OpenTrader;
using System.Windows.Markup;
using Newtonsoft.Json.Linq;
using Accord;
using ILGPU.Backends.PTX;
using System.Collections;

namespace OpenCompiler
{
    internal class Compiler
    {
        internal int line;
        internal int column;


        int pos;
        List<Token> tokens = new List<Token>();
        List<Symbol> symbols = new List<Symbol>();
        List<Instruction> instructions = new List<Instruction>();
        List<SymbolType> types = new List<SymbolType>() { SymbolType.String, SymbolType.Int, SymbolType.Float, SymbolType.Pane, SymbolType.Series };
        List<Enum> enums = new List<Enum>();

        Block? rootBlock;

        int CurrentFrame
        {
            get
            {
                var block = rootBlock;
                while (block.Next != null)
                {
                    block = block.Next;
                }
                return block.Frame;
            }
        }

        internal List<Instruction> Instructions
        {
            get { return instructions; }
        }

        internal List<Symbol> Symbols
        {
            get { return symbols; }
        }

        private List<Class> classes = new List<Class>();

        private Token? look
        {
            get
            {
                var token = pos >= tokens.Count ? null : tokens[pos];
                if (token != null)
                {
                    if (line != token.Line)
                    {
                        line = token.Line;
                        instructions.Add(new Instruction(OpCode.noop, line, "line "+line));
                    }
                    
                    column = token.Column;
                }
                return token;
            }
        }

        private void Match(string match)
        {
            if (look != null && look.Source == match)
            {
                pos++;
            }
            else
            {
                throw new CompilerException(match + " expected", line, column);
            }
        }

        private void Match(string[] matches)
        {
            if (look != null)
            {
                foreach (var match in matches)
                {
                    if (look.Source == match)
                    {
                        pos++;
                        return;
                    }
                }
                throw new CompilerException(matches[0] + " expected", line, column);
            }
            else
            {
                throw new CompilerException(matches[0] + " expected", line, column);
            }
        }

        private void Next()
        {
            pos++;
            if (instructions.Count == 1080)
            {
                System.Diagnostics.Debug.WriteLine("line reached " + pos);
            }
        }

        private void Rewind()
        {
            pos--;
        }

        internal void Compile(List<Token> tokens)
        {
            instructions.Clear();

            symbols.Clear();

            this.tokens = tokens;

            if (tokens.Count == 0)
                return;

            pos = 0;

            if (look!.Source != "script")
                return;
            pos++;

            if (look.Source != "{")
                return;

            GlobalSymbols();
            RemoveStatementEnds();
            rootBlock = new Block(null);
            DoBlock(rootBlock);
            instructions.Add(new Instruction(OpCode.exit, 0));
        }

        void RemoveStatementEnds()
        {
            while (look.Source == "\n" || look.Source == ";")
            {
                Next();
            }
        }

        bool IsStatementEnd(string source)
        {
            return source == "\n" || source == ";" || source == ";";
        }

        void GlobalSymbols()
        {
            symbols.Add(new ProcSymbol("print", SymbolType.Void, new List<SymbolType>() { SymbolType.String }, true));
            symbols.Add(new ProcSymbol("sma", SymbolType.Series, new List<SymbolType>() { SymbolType.Series, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("stochastic", SymbolType.Series, new List<SymbolType>() { SymbolType.Series, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("ema", SymbolType.Series, new List<SymbolType>() { SymbolType.Series, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("periodHigh", SymbolType.Series, new List<SymbolType>() { SymbolType.Series, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("periodLow", SymbolType.Series, new List<SymbolType>() { SymbolType.Series, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("bollingerUpper", SymbolType.Series, new List<SymbolType>() { SymbolType.Series, SymbolType.Int, SymbolType.Float }, true));
            symbols.Add(new ProcSymbol("bollingerLower", SymbolType.Series, new List<SymbolType>() { SymbolType.Series, SymbolType.Int, SymbolType.Float }, true));
            symbols.Add(new ProcSymbol("close", SymbolType.Series, new List<SymbolType>() { }, true));
            symbols.Add(new ProcSymbol("volume", SymbolType.Series, new List<SymbolType>() { }, true));
            symbols.Add(new ProcSymbol("rgb", SymbolType.Series, new List<SymbolType>() { SymbolType.Int, SymbolType.Int, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("argb", SymbolType.Series, new List<SymbolType>() { SymbolType.Int, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("namedColor", SymbolType.Series, new List<SymbolType>() { SymbolType.String, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("createPane", SymbolType.Pane, new List<SymbolType>() { SymbolType.Int }, true));
            symbols.Add(new EnumSymbol("RatioType", new List<EnumValue>() { new("positiveSum", 0), new("positiveN", 1), new("expectedReturn", 2) }));
            symbols.Add(new EnumSymbol("MarketType", new List<EnumValue>() { new("all", 0), new("bear", 1), new("bull", 2) }));
            symbols.Add(new EnumSymbol("ChartType", new List<EnumValue>() { new("day", 2), new("week", 3) }));
            symbols.Add(new EnumSymbol("Color", new List<EnumValue>() {
                new("aliceBlue", unchecked((int)0xfff0f8ff)),
                new("antiqueWhite", unchecked((int)0xfffaebd7)),
                new("aqua", unchecked((int)0xff0dfffff)),
                new("aquamarine", unchecked((int)0xff7fffd4)),
                new("azure", unchecked((int)0xfff0ffff)),
                new("beige", unchecked((int)0xfff5ff5dc)),
                new("bisque", unchecked((int)0xffffe4c4)),
                new("black", unchecked((int)0xff000000)),
                new("blanchedAlmond", unchecked((int)0xffffebcd)),
                new("blue", unchecked((int)0xff000dff)),
                new("blueViolet", unchecked((int)0xff8a2be2)),
                new("brown", unchecked((int)0xffa52a2a)),
                new("burlyWood", unchecked((int)0xffdeb887)),
                new("cadetBlue", unchecked((int)0xff5f9ea0)),
                new("chartreuse", unchecked((int)0xff7fff00)),
                new("chocolate", unchecked((int)0xffd2691e)),
                new("coral", unchecked((int)0xffff7f50)),
                new("cornflowerBlue", unchecked((int)0xff6495ed)),
                new("cornsilk", unchecked((int)0xfffff8dc)),
                new("crimson", unchecked((int)0xffdc143c)),
                new("cyan", unchecked((int)0xff0dffff)),
                new("darkBlue", unchecked((int)0xff0d0d8b)),
                new("darkCyan", unchecked((int)0xff0d8b8b)),
                new("darkGoldenRod", unchecked((int)0xffb8860b)),
                new("darkGray", unchecked((int)0xffa9a9a9)),
                new("darkGreen", unchecked((int)0xff006400)),
                new("darkKhaki", unchecked((int)0xffbdb76b)),
                new("darkMagenta", unchecked((int)0xff8b008b)),
                new("darkOliveGreen", unchecked((int)0xff556b2f)),
                new("darkOrange", unchecked((int)0xffff8c00)),
                new("darkOrchid", unchecked((int)0xff9932cc)),
                new("darkRed", unchecked((int)0xff8b0000)),
                new("darkSalmon", unchecked((int)0xffe9967a)),
                new("darkSeaGreen", unchecked((int)0xff8fbc8f)),
                new("darkSlateBlue", unchecked((int)0xff483d8b)),
                new("darkTurquoise", unchecked((int)0xff0dced1)),
                new("darkViolet", unchecked((int)0xff9400d3)),
                new("deepPink", unchecked((int)0xffff1493)),
                new("deepSkyBlue", unchecked((int)0xff00bfff)),
                new("dimGray", unchecked((int)0xff696969)),
                new("green", unchecked((int)0xff0d8000)),
                new("orange", unchecked((int)0xffffa500)),
                new("lightGreen", unchecked((int)0xff90ee90)),
                new("pink", unchecked((int)0xffffc0cb)),
                new("purple", unchecked((int)0xff800080)),
                new("red", unchecked((int)0xffff0000)),
                new("transparent", unchecked((int)0x00ffffff))}));
            var lineSymbol = new EnumSymbol("Line", new List<EnumValue>() {new("histogram", 0), new("dashes", 1), new("solid", 2), new("dots", 3)});
            symbols.Add(lineSymbol);
            var extremaSymbol = new EnumSymbol("Extrema", new List<EnumValue>() { new("peak", 0),  new("trough", 1) });
            symbols.Add(extremaSymbol);
            symbols.Add(new ProcSymbol("description", SymbolType.String, new List<SymbolType>() { SymbolType.Series }, true ));
            symbols.Add(new ProcSymbol("drawLabel", SymbolType.Void, new List<SymbolType>() { SymbolType.Pane, SymbolType.String, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("plotSeries", SymbolType.Void, new List<SymbolType>() { SymbolType.Pane, SymbolType.Series, SymbolType.Int, SymbolType.Int, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("plotSeriesFillBand", SymbolType.Void, new List<SymbolType>() { SymbolType.Pane, SymbolType.Series, SymbolType.Series, SymbolType.Int, SymbolType.Int, SymbolType.Int, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("plotSeriesOscillator", SymbolType.Void, new List<SymbolType>() { SymbolType.Pane, SymbolType.Series, SymbolType.Float, SymbolType.Float, SymbolType.Int, SymbolType.Int, SymbolType.Int, SymbolType.Int, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("pricePane", SymbolType.Pane, new List<SymbolType>() {}, true));
            symbols.Add(new ProcSymbol("volumePane", SymbolType.Pane, new List<SymbolType>() { }, true));
            symbols.Add(new ProcSymbol("chartType", SymbolType.Int, new List<SymbolType>() {}, true ));
            symbols.Add(new ProcSymbol("candleWidth", SymbolType.Int, new List<SymbolType>() { }, true));
            symbols.Add(new ProcSymbol("getCandles", SymbolType.Candles, new List<SymbolType>() { SymbolType.Int, SymbolType.Int }, true));
            symbols.Add(new ProcSymbol("annotateBar", SymbolType.Void, new List<SymbolType>() { SymbolType.String, SymbolType.Int, SymbolType.Bool, SymbolType.Int, SymbolType.Int }, true)); // (string text, int bar, bool above, System.Drawing.Color fontcolor, System.Drawing.Color background, Font? font=null)
            symbols.Add(new ProcSymbol("findProfitable", SymbolType.Array, new List<SymbolType>() { SymbolType.Candles, SymbolType.Int, SymbolType.Int, SymbolType.Int, SymbolType.Float, SymbolType.Int }, true));

            classes.Add(new Class("Candle")
            {
                Symbols = new List<Symbol>() { new Symbol("name", SymbolType.String), new Symbol("bar", SymbolType.Int) }
            });

        }
        private void DoBlock(Block block)
        {
            Match("{");
            RemoveStatementEnds();
            if (block.Previous != null)
            {
                instructions.Add(new Instruction(OpCode.pushf, 0, "Create frame for new block"));
            }
            if (look == null)
                return;
            bool declarations = true;

            while (declarations)
            {
                switch (look.Source)
                {
                    case "var":
                        VariableDefinition();
                        break;
                    case "enum":
                        EnumDefinition(block);
                        break;
                    case "proc":

                        if (block.Previous == null)
                        {
                            int jmp = instructions.Count;
                            instructions.Add(new Instruction(OpCode.bra, 0)); // This ensures that any declarations are handled
                            ProcedureDefinition(new Block(block));
                            instructions[jmp] = new Instruction(OpCode.bra, instructions.Count);
                        }
                        else
                        {
                            throw new CompilerException("proc can only be defined at the top level", line, column);
                        }
                        break;
                    default:
                        declarations = false;
                        break;
                }
                RemoveStatementEnds();
            }


            while (look.Source != "}")
            {
                switch (look.Source)
                {
                    case "var":
                        VariableDefinition();
                        break;
                    default:
                        Statement(block);
                        break;
                }
                RemoveStatementEnds();
            }
            Match("}");

            if (block.Previous != null)
            {
                symbols.RemoveAll(s => s.Block != null && s.Block.Frame > block.Frame);
                block.Rewind();
                instructions.Add(new Instruction(OpCode.popf, 0, "Pop frame when exiting block"));
            }
        }


        void ReturnType(ProcSymbol symbol)
        {
            Next();
            if (look != null && look.Source == ":")
            {
                Match(":");
                if (look != null)
                {
                    switch (look.Source)
                    {
                        case SymbolType.StringSymbol:
                            symbol.ReturnType = SymbolType.String;
                            break;
                        case SymbolType.IntSymbol:
                            symbol.ReturnType = SymbolType.Int;
                            break;
                        case SymbolType.FloatSymbol:
                            symbol.ReturnType = SymbolType.Float;
                            break;
                        case SymbolType.BoolSymbol:
                            symbol.ReturnType = SymbolType.Bool;
                            break;
                        case SymbolType.PaneSymbol:
                            symbol.ReturnType = SymbolType.Pane;
                            break;
                        case SymbolType.SeriesSymbol:
                            symbol.ReturnType = SymbolType.Series;
                            break;
                        default:
                            throw new CompilerException(look.Source + " is not a valid return type", line, column);
                    }
                    Next();
                }
            }
            else
            {
                symbol.ReturnType = SymbolType.Void;
            }
        }

        private void CreateParams(ProcSymbol procSymbol, Block block)
        {
            Match("(");

            while (look != null && look.Source != ")")
            {
                if (look.Group == TokenGroup.identifier)
                {
                    var symbol = symbols.Find(s => s.Name == look.Source && s.Block.Frame == block.Frame);

                    if (symbol == null)
                    {
                        symbol = new Symbol(look.Source, SymbolType.Void);
                        symbol.Block = block;
                        Next();
                        Match(":");

                        switch (look.Source)
                        {
                            case SymbolType.StringSymbol:
                                symbol.Type = SymbolType.String;
                                procSymbol.Parameters.Add(SymbolType.String);
                                Next();
                                break;
                            case SymbolType.IntSymbol:
                                symbol.Type = SymbolType.Int;
                                procSymbol.Parameters.Add(SymbolType.Int);
                                Next();
                                break;
                            case SymbolType.BoolSymbol:
                                symbol.Type = SymbolType.Bool;
                                procSymbol.Parameters.Add(SymbolType.Bool);
                                Next();
                                break;
                            case SymbolType.FloatSymbol:
                                symbol.Type = SymbolType.Float;
                                procSymbol.Parameters.Add(SymbolType.Float);
                                Next();
                                break;
                            case SymbolType.SeriesSymbol:
                                symbol.Type = SymbolType.Series;
                                procSymbol.Parameters.Add(SymbolType.Series);
                                Next();
                                break;
                            case SymbolType.PaneSymbol:
                                symbol.Type = SymbolType.Pane;
                                procSymbol.Parameters.Add(SymbolType.Pane);
                                Next();
                                break;
                            default:
                                var enumSymbol = symbols.Find(e => e.Name == look.Source && e.Type == SymbolType.Enum);
                                if (enumSymbol == null)
                                {
                                    throw new CompilerException("Bad type " + look.Source + " in call list of " + symbol.Name, line, column);
                                }
                                else {
                                    procSymbol.Parameters.Add(SymbolType.Enum);
                                }
                                break;
                        }
                        symbols.Add(symbol);

                        if (look.Source == null || look.Source != ",")
                        {
                            break;
                        }
                        else
                        {
                            Next();
                        }
                    }
                }
            }
            Match(")");
        }


        private void Return(Block fromBlock)
        {
            // Rewind the frames
            Block? block = fromBlock;
            while (block != null & !block!.IsBase)
            {
                instructions.Add(new Instruction(OpCode.popf, 0, "Pop frame when returning block"));
                block = block.Previous;
            }
            // instructions.Add(new Instruction(OpCode.popf, 0));

            // Return
            instructions.Add(new Instruction(OpCode.ret, 0, "Return from Block"));
        }

        private void ProcedureDefinition(Block block)
        {
            Match("proc");
            if (look != null && look.Group == TokenGroup.identifier)
            {
                var symbol = symbols.Find(s => s.Name == look.Source);
                if (symbol == null)
                {
                    instructions.Add(new Instruction(OpCode.noop, 1, "define proc " + look.Source));
                    var procSymbol = new ProcSymbol(look.Source, SymbolType.Void);
                    procSymbol.Address = instructions.Count;
                    symbols.Add(procSymbol);

                    Next();

                    var paramBlock = new Block(block)
                    {
                        IsBase = true,
                    };

                    paramBlock.Frame = paramBlock.Frame + 1;
                    CreateParams(procSymbol, block);

                    procSymbol.Offset = instructions.Count;
                    if (look != null & look.Source == ":")
                    {
                        ReturnType(procSymbol);
                    }
                    else
                    {
                        procSymbol.ReturnType = SymbolType.Void;
                    }

                    DoBlock(paramBlock);
                    Return(paramBlock);
                    instructions.Add(new Instruction(OpCode.noop, 1, "end proc " + procSymbol.Name));
                    // instructions.Add(new Instruction(OpCode.ret, 0));
                }
            }
            RemoveStatementEnds();
            symbols.RemoveAll(s => s.Block != null && s.Block.Frame > block.Frame);
        }

        private void EnumDefinition(Block block)
        {
            Match("enum");
            if (look == null)
            {
                throw new CompilerException("identifier expected", line, column);
            }

            if (look.Group == TokenGroup.identifier)
            {
                var found = symbols.Find(s => s.Name == look.Source);
                if (found == null)
                {
                    var enumSymbol = new EnumSymbol(look.Source);
                    Next();

                    Match("{");

                    while (look.Group == TokenGroup.identifier)
                    {
                        var name = look.Source;
                        Next();

                        Match("=");

                        if (look.Group == TokenGroup.literal)
                        {
                            var constant = int.Parse(look.Source);
                            enumSymbol.Values.Add(new(name, constant));
                        }

                        if (look.Source != ",")
                            break;
                    }

                    Match("}");
                }
            }

            if (look.Source == ";" || look.Source == "\n")
            {
                Match(new string[] { ";", "\n", "}" });
            }
        }

        private void VariableDefinition()
        {
            Match("var");
            if (look == null)
            {
                throw new CompilerException("identifier expected", line, column);
            }

            if (look.Group == TokenGroup.identifier)
            {
                var found = symbols.Find(s => s.Name == look.Source);
                if (found == null)
                {
                    var symbol = new Symbol(look.Source, SymbolType.Int);
                    symbol.Block = Block.CurrentBlock;
                    instructions.Add(new Instruction(OpCode.frame, 0));
                    instructions.Add(new Instruction(OpCode.alloc, (int)1, "Allocate space for variable"));
                    Next();

                    Match(":");

                    switch (look.Source)
                    {
                        case "string":
                            symbol.Type = SymbolType.String;
                            Next();
                            break;
                        case "int":
                            symbol.Type = SymbolType.Int;
                            Next();
                            break;
                        case "float":
                            symbol.Type = SymbolType.Float;
                            Next();
                            break;
                        case "bool":
                            symbol.Type = SymbolType.Bool;
                            Next();
                            break;
                        case "pane":
                            symbol.Type = SymbolType.Pane;
                            Next();
                            break;
                        case "series":
                            symbol.Type = SymbolType.Series;
                            Next();
                            break;
                        case "Candles":
                            symbol.Type = SymbolType.Candles;
                            Next();
                            break;
                        case "Candle":
                            symbol.Type = SymbolType.Candle;
                            Next();
                            break;
                        case "[":
                            symbol.Type = SymbolType.Array;
                            symbol =  symbol.ToArraySymbol();
                            ArrayDefinition(symbol as ArraySymbol);
                            break;
                        default:
                            var @enum = symbols.Find(e => e.Name == look.Source && e is EnumSymbol);
                            if (@enum == null){
                                throw new CompilerException("type expected", line, column);
                            }
                            else
                            {
                                symbol.Type = new EnumType(@enum as EnumSymbol);
                            }
                            Next();
                            break;
                    }

                    


                    if (look.Source == ";" || look.Source == "\n")
                    {
                        symbols.Add(symbol);
                        symbol.Offset = Block.CurrentBlock.Offset;
                        Block.CurrentBlock.Add(1);
                    }
                    else if (look.Source == "=")
                    {
                        symbols.Add(symbol);
                        symbol.Offset = Block.CurrentBlock.Offset;
                        Block.CurrentBlock.Add(1);
                        Assignment(symbol);
                    } else
                    {
                        throw new CompilerException("; or = expected", line, column);
                    }

                }
                else
                {
                    throw new CompilerException("Identifier '"+look.Source+"' already declared", line, column);
                }
            }
            else
            {
                throw new CompilerException("Identifier expected", line, column);
            }

            if (look.Source == ";" || look.Source == "\n")
            {
                Match(new string[] { ";", "\n", "}" });
            }
        }

        void ArrayDefinition(ArraySymbol symbol)
        {
            Match("[");

            switch (look.Source)
            {
                case SymbolType.StringSymbol:
                case SymbolType.IntSymbol:
                case SymbolType.FloatSymbol:
                    symbol.ReturnType = SymbolType.Float;
                    break;
                case SymbolType.BoolSymbol:
                    symbol.ReturnType = SymbolType.Bool;
                    break;
                case SymbolType.SeriesSymbol:
                    symbol.ReturnType = SymbolType.Series;
                    break;
                case SymbolType.PaneSymbol:
                    symbol.ReturnType = SymbolType.Pane;
                    break;
                case SymbolType.CandleSymbol:
                    symbol.ReturnType = SymbolType.Candle;
                    break;
                case SymbolType.PatternSymbol:
                    symbol.ReturnType = SymbolType.Pattern;
                    break;
                default:
                    throw new CompilerException("Basic type for array expected", line, column);
            }
            Next();
            Match("]");
        }
            

        void Assignment(Symbol symbol)
        {
            Match("=");

            switch (symbol.Type.Name)
            {
                case SymbolType.StringSymbol:
                    StringExpression();
                    break;
                case SymbolType.IntSymbol:
                    Expression();
                    break;
                case SymbolType.FloatSymbol:
                    Expression();
                    break;
                case SymbolType.BoolSymbol:
                    BoolExpression();
                    break;
                case SymbolType.PaneSymbol:
                    PaneExpression();
                    break;
                case SymbolType.SeriesSymbol:
                    Expression();
                    break;
                case SymbolType.ArraySymbol:
                    ArrayExpression();
                    break;
                default:
                    Expression();
                    break;
            }

            instructions.Add(new Instruction(OpCode.frame, Block.CurrentBlock.Frame - symbol.Block.Frame));
            instructions.Add(new Instruction(OpCode.store, symbol.Offset, "Assignment to "+symbol.Name));
        }

        void CallProcedure(ProcSymbol symbol, Symbol? classSymbol=null)
        {
            Match("(");
            int i = 0;

            instructions.Add(new Instruction(OpCode.pushf, 0,"Create new frame for procedure call to "+symbol.Name));
            Block block = new(Block.CurrentBlock);

            if (classSymbol != null)
            {
                instructions.Add(new Instruction(OpCode.frame, Block.CurrentBlock.Frame - classSymbol.Block.Frame));
                instructions.Add(new Instruction(OpCode.load, classSymbol.Offset));
                instructions.Add(new Instruction(OpCode.frame, 0, "Set current frame for class parameter " + i.ToString()));
                instructions.Add(new Instruction(OpCode.alloc, 1));
                instructions.Add(new Instruction(OpCode.store, i, "Store " + symbol.Parameters[i] + " parameter"));

                i++;
            }

            while (look != null && look.Source != ")")
            {
                var symbolType = symbol.Parameters[i];
                switch (symbolType.Name)
                {
                    case SymbolType.StringSymbol:
                        StringExpression();
                        break;
                    case SymbolType.IntSymbol:
                    case SymbolType.FloatSymbol:
                        Expression();
                        break;
                    case SymbolType.SeriesSymbol:
                        Expression();
                        break;
                    case SymbolType.BoolSymbol:
                        BoolExpression();
                        break;
                    case SymbolType.PaneSymbol:
                        PaneExpression();
                        break;
                    case SymbolType.ProcSymbol:
                        Expression();
                        break;
                    default:
                        if (symbolType is EnumType)
                        {
                            Expression();
                        }
                        break;
                }

                instructions.Add(new Instruction(OpCode.frame, 0,"Set current frame for parameter "+i.ToString()));
                instructions.Add(new Instruction(OpCode.alloc, 1));
                instructions.Add(new Instruction(OpCode.store, i,"Store "+ symbol.Parameters[i].Name + " parameter"));

                if (look.Source == null || look.Source != "," )
                {
                    break;
                }
                else
                {
                    Next();
                }
                i++;
            }
            Match(")");

            if (symbol.Bridge)
            {
                if (classSymbol == null)
                {
                    instructions.Add(new Instruction(OpCode.callext, symbol.Name));
                }
                else
                {
                    instructions.Add(new Instruction(OpCode.callext, symbol.Name));
                }
            }
            else
            {
                instructions.Add(new Instruction(OpCode.bsr, symbol.Address));
            }

            instructions.Add(new Instruction(OpCode.popf, 0, "Pop frame on exit from procedure " + symbol.Name));
            Block.CurrentBlock.Rewind();
            RemoveStatementEnds();
        }

        void Statement(Block currentBlock)
        {
            if (look == null)
                throw new CompilerException("statement expected", line, column);

            switch (look.Source)
            {
                case "if":
                    DoIf(currentBlock) ;
                    break;
                case "while":
                    DoWhile (currentBlock) ;
                    break;
                case "return":
                    Next();
                    Return (currentBlock);
                    break;
                case "for":
                    DoFor(currentBlock);
                    break;
                default:
                    var symbol = symbols.Find(s=>s.Name == look!.Source);
                    if (symbol == null)
                    {
                        throw new CompilerException(look!.Source + " is not a valid identifier",line,column);
                    }
                    Next();
                    if (symbol.Type == SymbolType.Proc)
                    {
                        var procSymbol = symbol as ProcSymbol;
                        if (procSymbol != null && procSymbol.ReturnType == SymbolType.Void)
                        {
                            CallProcedure(procSymbol);
                        }
                        else
                        {
                            throw new CompilerException("proc "+symbol.Name+" must have return type void",line,column);
                        }
                    }
                    else 
                    {
                        Assignment(symbol);
                        instructions.Add(new Instruction(OpCode.store, symbol.Offset,symbol.Name));
                    }
                    break;
            }
        }

        void DoIf(Block currentBlock)
        {
            Match("if");
            BoolExpression();
            int ifAddress = instructions.Count;
            instructions.Add(new Instruction(OpCode.brfalse, (int)0)); // this is a dummy to be replaced in a few lines
            var ifBlock = new Block(currentBlock);
            DoBlock(ifBlock);
            if (look.Source == "else")
            {
                Next();
                int endThenAddress = instructions.Count;  // this is the instruction on the next line
                instructions.Add(new Instruction(OpCode.bra, (int)0)); 
                instructions[ifAddress] = new Instruction(OpCode.brfalse, instructions.Count); // replace with actual branch
                var elseBlock = new Block(currentBlock);
                DoBlock(elseBlock);
                instructions[endThenAddress] = new Instruction(OpCode.bra, instructions.Count); // replace if branch
            } else
            {
                instructions[ifAddress] = new Instruction(OpCode.brfalse, instructions.Count); // replace if branch
            }        
        }

        void DoWhile(Block currentBlock)
        {
            Match("while");
            int whileAddress = instructions.Count;
            BoolExpression();
            int breakAddress = instructions.Count;
            instructions.Add(new Instruction(OpCode.brtrue, (int)0)); // this is a dummy to be replaced later
            DoBlock(currentBlock);
            instructions.Add(new Instruction(OpCode.bra, whileAddress));
            instructions[breakAddress] = new Instruction(OpCode.brfalse, instructions.Count);  // replace break
        }


        void DoFor(Block currentBlock)
        {
            Match("for");
            var found = symbols.Find(s => s.Name == look.Source);
            if (found == null)
            {
                var forBlock = new Block(currentBlock);
                var elementSymbol = new Symbol(look.Source, SymbolType.Int); // Symbol.Int is a placeholder till we figure out the type
                symbols.Add(elementSymbol);

                elementSymbol.Block = forBlock;
                instructions.Add(new Instruction(OpCode.pushf, 0, "Create new frame for array for block"));
                instructions.Add(new Instruction(OpCode.frame, 0));
                instructions.Add(new Instruction(OpCode.alloc, (int)1, "Allocate space for array element"));
                const int ARRAY_ELEMENT = 0;

                Next();
                Match("in");

                // TODO: 
                var backpos = pos;
                try
                {
                    // set up a new frame to carry the array element, array, and array iterator for the for loop
                    instructions.Add(new Instruction(OpCode.alloc, (int)1, "Allocate space for array"));
                    const int ARRAY = 1;
                    instructions.Add(new Instruction(OpCode.alloc, (int)1, "Allocate space for internal iterator"));
                    const int ARRAY_ITERATOR = 2;

                    // var guid = Guid.NewGuid().ToString();
                    // var iterator = "index_" + guid.Replace("-", "_");
                    // var iteratorSymbol = new ProcSymbol(look.Source, SymbolType.Int, new List<SymbolType>(), false);

                    elementSymbol.Type = ArrayExpression();  // array is loaded in register
                    instructions.Add(new Instruction(OpCode.frame, 0));
                    instructions.Add(new Instruction(OpCode.store, ARRAY)); // store register into array
         

                    instructions.Add(new Instruction(OpCode.move, 0)); // load 0 into register
                    instructions.Add(new Instruction(OpCode.store, ARRAY_ITERATOR)); // store register into iterator

                    // Block block = new(Block.CurrentBlock); // Set up call to getCount()
                    int forAddress = instructions.Count;
                    instructions.Add(new Instruction(OpCode.pushf, 0, "Create new frame for call to getCount()"));
                    // calculate the for expression
                    // load the object
                    instructions.Add(new Instruction(OpCode.frame, 0));
                    instructions.Add(new Instruction(OpCode.alloc, (int)1, "getCount() call"));
                    instructions.Add(new Instruction(OpCode.frame, 1));
                    instructions.Add(new Instruction(OpCode.load, ARRAY, "ARRAY"));
                    instructions.Add(new Instruction(OpCode.frame, 0));
                    instructions.Add(new Instruction(OpCode.store, 0));
                    // get the member
                    instructions.Add(new Instruction(OpCode.callext, "getCount"));
                    instructions.Add(new Instruction(OpCode.popf, 0)); // Release frame for call to get count
                    // Block.CurrentBlock.Rewind();

                    instructions.Add(new Instruction(OpCode.pshr, 0)); // push array count onto the stack
                    instructions.Add(new Instruction(OpCode.frame, 0));
                    instructions.Add(new Instruction(OpCode.load, ARRAY_ITERATOR, "ARRAY_ITERATOR")); // load iterator of current frame
                    instructions.Add(new Instruction(OpCode.cmpr, 0)); // compare the stack with the register, leaving the result in the register
                    instructions.Add(new Instruction(OpCode.pop, 0)); // pop the stack doing nothing with it
                    instructions.Add(new Instruction(OpCode.sge, 0));
                    int testAddress = instructions.Count;
                    instructions.Add(new Instruction(OpCode.brfalse, 0)); // get out of it's >= getCount()

                    // Now store the current element
                    instructions.Add(new Instruction(OpCode.pushf, 0, "Create new frame for call to getElement()"));
                    instructions.Add(new Instruction(OpCode.frame, 0));
                    instructions.Add(new Instruction(OpCode.alloc, (int)1, "first parameter"));
                    instructions.Add(new Instruction(OpCode.frame, 1));
                    instructions.Add(new Instruction(OpCode.load, ARRAY, "ARRAY"));
                    instructions.Add(new Instruction(OpCode.frame, 0));
                    instructions.Add(new Instruction(OpCode.store, 0));
                    instructions.Add(new Instruction(OpCode.frame, 0));
                    instructions.Add(new Instruction(OpCode.alloc, (int)1, "second parameter"));
                    instructions.Add(new Instruction(OpCode.frame, 1));
                    instructions.Add(new Instruction(OpCode.load, ARRAY_ITERATOR, "ARRAY_ITERATOR"));
                    instructions.Add(new Instruction(OpCode.frame, 0));
                    instructions.Add(new Instruction(OpCode.store, 1));
                    instructions.Add(new Instruction(OpCode.callext, "getElement"));
                    instructions.Add(new Instruction(OpCode.popf, 0)); // Release frame for call to get count

                    instructions.Add(new Instruction(OpCode.frame, 0));
                    instructions.Add(new Instruction(OpCode.store, ARRAY_ELEMENT));

                    var doBlock = new Block(forBlock);
                    DoBlock(forBlock);
                    doBlock.Rewind();
                    instructions.Add(new Instruction(OpCode.frame, 0));
                    instructions.Add(new Instruction(OpCode.load, ARRAY_ITERATOR, "ARRAY_ITERATOR")); // load register from iterator of current frame
                    instructions.Add(new Instruction(OpCode.pshr, 0));
                    instructions.Add(new Instruction(OpCode.move, 1));
                    instructions.Add(new Instruction(OpCode.add, 0));
                    instructions.Add(new Instruction(OpCode.popr, 0));
                    instructions.Add(new Instruction(OpCode.store, ARRAY_ITERATOR, "ARRAY_ITERATOR"));
                    instructions.Add(new Instruction(OpCode.bra, forAddress));
                    instructions[testAddress] = new Instruction(OpCode.brfalse, instructions.Count);
                    instructions.Add(new Instruction(OpCode.popf, 0)); // remove frame we created for the array
                } 
                catch(Exception ex)
                {
                    pos = backpos;
                    throw new CompilerException("range '" + look.Source + "' not implemented", line, column);
                }
                Block.CurrentBlock.Rewind();
            }
            else
            {
                throw new CompilerException("Identifier '" + look.Source + "' already declared", line, column);
            }
        }

        bool IsOrOp(string op)
        {
            return op == "|" || op == "^";
        }

        void PaneExpression()
        {
            Factor();
        }

        SymbolType ArrayExpression()
        {
            return ArrayFactor();
        }

        void BoolExpression()
        {
            BoolTerm();
            while (IsOrOp(look.Source))
            {
                instructions.Add(new Instruction(OpCode.pshr, 0));
                switch (look.Source)
                {
                    case "|":
                        BoolOr();
                        break;
                }
            }
        }

        void BoolOr()
        {
            Match("|");
            BoolTerm();
            instructions.Add(new Instruction(OpCode.or, (int)0));
            instructions.Add(new Instruction(OpCode.pop, (int)1));
        }


        void BoolTerm()
        {
            NotFactor();
            while (look.Source == "&")
            {
                instructions.Add(new Instruction(OpCode.pshr, 0));
                Match("&");
                NotFactor();
                instructions.Add(new Instruction(OpCode.and, 0));
                instructions.Add(new Instruction(OpCode.pop,0));
            }
        }

        void NotFactor()
        {
            if (look != null && look.Source == "!") {
                Match("!)");
                BoolFactor();
                instructions.Add(new Instruction(OpCode.not, 0));
                instructions.Add(new Instruction(OpCode.pop, 0));
            }
            else
            {
                BoolFactor();
            }
        }


        void BoolFactor()
        {
            if (look != null && look.Source == "true")
            {
                instructions.Add(new Instruction(OpCode.move, true));
                Next();
            }
            else if (look != null && look.Source == "false")
            {
                instructions.Add(new Instruction(OpCode.move, false));
                Next();
            } 
            else
            {
                Relation();
            }
        }

        void Equals()
        {
            Match("==");
            Expression();
            instructions.Add(new Instruction(OpCode.cmpr, 0));
            instructions.Add(new Instruction(OpCode.popr, 0));
            instructions.Add(new Instruction(OpCode.seq, 0));
        }

        void NotEquals()
        {
            Match("!=");
            Expression();
            instructions.Add(new Instruction(OpCode.cmpr, 0));
            instructions.Add(new Instruction(OpCode.popr, 0));
            instructions.Add(new Instruction(OpCode.sne, 0));
        }

        void Less()
        {
            Match("<");
            Expression();
            instructions.Add(new Instruction(OpCode.cmpr, 0));
            instructions.Add(new Instruction(OpCode.popr, 0));
            instructions.Add(new Instruction(OpCode.slt, 0));
        }

        void LessEquals()
        {
            Match("<=");
            Expression();
            instructions.Add(new Instruction(OpCode.cmpr, 0));
            instructions.Add(new Instruction(OpCode.popr, 0));
            instructions.Add(new Instruction(OpCode.sle, 0));
        }

        void Greater()
        {
            Match(">");
            Expression();
            instructions.Add(new Instruction(OpCode.cmpr, 0));
            instructions.Add(new Instruction(OpCode.popr, 0));
            instructions.Add(new Instruction(OpCode.sgt, 0));
        }

        void GreaterEquals()
        {
            Match(">=");
            Expression();
            instructions.Add(new Instruction(OpCode.cmpr, 0));
            instructions.Add(new Instruction(OpCode.popr, 0));
            instructions.Add(new Instruction(OpCode.sge, 0));
        }



        void Concat()
        {
            Match("+");
            Term();
            instructions.Add(new Instruction(OpCode.add, 0));
            instructions.Add(new Instruction(OpCode.pop, 0));
        }

        SymbolType ArrayFactor()
        {
            if (look == null)
            {
                throw (new CompilerException("literal or procedure expected", line, column));
            }

            if (look.Source == "[")
            {
                instructions.Add(new Instruction(OpCode.callext, "newArray"));
                return null;
                Next();
            }
            else
            {
                var symbol = symbols.FindLast(s => s.Name == look!.Source);

                if (symbol == null)
                {
                    throw (new CompilerException("literal or identifier expected", line, column));
                }

                switch (symbol.Type.Name)
                {
                    case SymbolType.ArraySymbol:
                        Next();
                        instructions.Add(new Instruction(OpCode.frame, Block.CurrentBlock.Frame - symbol.Block.Frame));
                        instructions.Add(new Instruction(OpCode.load, symbol.Offset, symbol.Name));
                        return (symbol as ArraySymbol).ReturnType;
                        break;
                    case SymbolType.CandlesSymbol:
                        Next();
                        Match(".");
                        var proc = symbols.FindLast(s => s.Name == look!.Source);
                        Next();
                        CallProcedure(proc as ProcSymbol,symbol);
                        return (proc as ProcSymbol).ReturnType;
                        break;
                    case SymbolType.ProcSymbol:
                        Next();
                        CallProcedure(symbol as ProcSymbol);
                        return (symbol as ProcSymbol).ReturnType;
                        break;
                    default:
                        throw (new CompilerException("[] or procedure expected", line, column));
                }
            }
        }


        void ClassMember(Symbol classSymbol) // Is coming from a factor. At this point classes can only have methods.
        {
            Match(".");

            var @class = classes.Find(c=>c.Name == classSymbol.Type.Name);

            if (@class == null)
            {
                throw (new CompilerException("class now found", line, column));
            }

            var symbol = @class.Symbols.FindLast(s => s.Name == look!.Source);

            if (symbol == null)
            {
                throw (new CompilerException("literal or identifier expected", line, column));
            }

            switch (symbol.Type.Name)
            {
                case SymbolType.StringSymbol:
                case SymbolType.IntSymbol:
                    Rewind();
                    ClassAccessor(classSymbol);
                    break;
                case SymbolType.ProcSymbol:
                    Next();
                    CallProcedure(symbol as ProcSymbol,classSymbol);
                    break;
                default:
                    throw (new CompilerException("class procedure expected", line, column));
            }
        }

        void StringFactor()
        {
            if (look != null && look.Group == TokenGroup.literal)
            {
                instructions.Add(new Instruction(OpCode.move, look.Source));
                Next();
            }
            else
            {
                if (look == null)
                {
                    throw (new CompilerException("literal or identifier expected",line,column));
                }

                var symbol = symbols.FindLast(s => s.Name == look!.Source);

                if (symbol == null)
                {
                    throw (new CompilerException("literal or identifier expected", line, column));
                }

                switch (symbol.Type.Name)
                {
                    case SymbolType.CandleSymbol:
                    case SymbolType.CandlesSymbol:
                        Next();
                        if (look.Source == ".")
                        {
                            ClassMember(symbol);
                        }
                        else
                        {
                            Rewind();
                            Identifier(symbol);
                        }
                        break;
                    case SymbolType.ProcSymbol:
                        Next();
                        CallProcedure(symbol as ProcSymbol);
                        break;
                    case SymbolType.IntSymbol:
                    case SymbolType.StringSymbol:
                    case SymbolType.BoolSymbol:
                    case SymbolType.FloatSymbol:
                        Identifier(symbol);
                        break;
                    default:
                        throw (new CompilerException("literal or procedure expected", line, column));
                }
            }
        }

        void StringExpression()
        {
            StringFactor();
            while (look != null && look.Source == "+")
            {
                instructions.Add(new Instruction(OpCode.pshr, 0));
                Concat();
            }
        }

        void Expression()
        {
            Term();
            while (look != null && new string[] {"+", "-" }.Contains(look.Source))
            {
                instructions.Add(new Instruction(OpCode.pshr, 0));
                switch (look.Source)
                {
                    case "+":
                        Add();
                        break;
                    case "-":
                        Subtract();
                        break;
                }
            }
        }

        void Multiply()
        {
            Match("*");
            instructions.Add(new Instruction(OpCode.pshr, 0));
            Factor();
            instructions.Add(new Instruction(OpCode.mul, 0));
            instructions.Add(new Instruction(OpCode.popr, 0));
        }

        void Divide()
        {
            Match("/");
            instructions.Add(new Instruction(OpCode.pshr, 0));
            Factor();
            instructions.Add(new Instruction(OpCode.exch, 0));
            instructions.Add(new Instruction(OpCode.div, 0));
            instructions.Add(new Instruction(OpCode.popr, 0));
        }


        void SignedFactor()
        {
            if (look != null && look.Source == "+")
            {
                Match("+");
            } else if (look != null && look.Source == "-") {
                Factor();
                instructions.Add(new Instruction(OpCode.neg, 0));
            }
            else
            {
                Factor();
            }
        }

        void Term()
        {
            SignedFactor();
            while (look != null && look.Source == "*" || look.Source == "/")
            {
                if (look != null && look.Source == "*")
                {
                    Multiply();
                }
                else if (look != null && look.Source == "/")
                {
                    Divide();
                }
            }
        }

        void Add()
        {
            Match("+");
            Term();
            instructions.Add(new Instruction(OpCode.add, 0));
            instructions.Add(new Instruction(OpCode.popr, 0));
        }

        void Subtract()
        {
            Match("-");
            Term();
            // instructions.Add(new Instruction(OpCode.exch, 0));
            instructions.Add(new Instruction(OpCode.sub, 0));
            instructions.Add(new Instruction(OpCode.popr, 0));
        }

        void Relation()
        {
            Expression();
            if (look != null && new string[]{"==","!=","<",">","<=",">="}.Contains(look.Source))
            {
                instructions.Add(new Instruction(OpCode.pshr, 0));
                switch(look.Source)
                {
                    case "==":
                        Equals();
                        break;
                    case "!=":
                        NotEquals();
                        break;
                    case "<":
                        Less();
                        break;
                    case "<=":
                        LessEquals();
                        break;
                    case ">":
                        Greater();
                        break;
                    case ">=":
                        GreaterEquals();
                        break;
                }
            }
        }

        void Identifier(Symbol symbol)
        {
            switch (symbol.Type.Name)
            {
                case SymbolType.ArraySymbol:
                    Next();
                    ArrayAccessor(symbol as ArraySymbol);
                    break;
                case SymbolType.CandlesSymbol:
                case SymbolType.PatternSymbol:
                case SymbolType.ExtremaSymbol:
                    Next();
                    ClassAccessor(symbol);
                    break;
                default:
                    Next();
                    instructions.Add(new Instruction(OpCode.frame, Block.CurrentBlock.Frame - symbol.Block.Frame));
                    instructions.Add(new Instruction(OpCode.load, symbol.Offset, symbol.Name));
                    break;
            }
        }

        void ArrayAccessor(ArraySymbol symbol)
        {
            Match("[");


            // add index 
            Block block = new(Block.CurrentBlock);
            instructions.Add(new Instruction(OpCode.pushf, 0, "Create new frame for array accessor to " + symbol.Name));
            // load the array object
            instructions.Add(new Instruction(OpCode.frame, Block.CurrentBlock.Frame - symbol.Block.Frame));
            instructions.Add(new Instruction(OpCode.load, symbol.Block.Offset));
            // store the object
            instructions.Add(new Instruction(OpCode.frame, 0));
            instructions.Add(new Instruction(OpCode.alloc, 1));
            instructions.Add(new Instruction(OpCode.store, 0));
            // load and store the type
            instructions.Add(new Instruction(OpCode.alloc, 1));
            instructions.Add(new Instruction(OpCode.move, symbol.ReturnType));
            instructions.Add(new Instruction(OpCode.store, 1, "Store "+symbol.ReturnType+" object"));
            Expression();
            instructions.Add(new Instruction(OpCode.frame, 0, "Set current frame for index"));
            instructions.Add(new Instruction(OpCode.alloc, 1));
            instructions.Add(new Instruction(OpCode.store, 0, "Store index"));
            // get the element
            instructions.Add(new Instruction(OpCode.callext, "getElement"));
            Match("]");
            Block.CurrentBlock.Rewind();
            Next();
        }

        void ClassAccessor(Symbol symbol)
        {
            Match(".");
            instructions.Add(new Instruction(OpCode.pushf, 0, "Create new frame for class accessor to " + symbol.Name));
            // load the object
            instructions.Add(new Instruction(OpCode.frame, Block.CurrentBlock.Frame - symbol.Block.Frame + 1));
            instructions.Add(new Instruction(OpCode.load, symbol.Offset));
            instructions.Add(new Instruction(OpCode.frame, 0));
            instructions.Add(new Instruction(OpCode.alloc, 1));
            // store the object
            instructions.Add(new Instruction(OpCode.store, 0));
            // load and store the type
            instructions.Add(new Instruction(OpCode.alloc, 1));
            instructions.Add(new Instruction(OpCode.move, symbol.Type.Name));
            instructions.Add(new Instruction(OpCode.store, 1));
            // load and store the member name
            instructions.Add(new Instruction(OpCode.alloc, 1));
            instructions.Add(new Instruction(OpCode.move, look.Source));
            instructions.Add(new Instruction(OpCode.store, 2));
            // get the member
            instructions.Add(new Instruction(OpCode.callext, "getMember"));
            instructions.Add(new Instruction(OpCode.popf, 0));
            Next();
        }


        void Factor()
        {
            if (look != null && look.Source == "(")
            {
                Match("(");
                Expression();
                Match(")");
            }
            else if (look != null && look.Group == TokenGroup.literal)
            {
                int intValue = 0;
                double doubleValue = 0;
                if (int.TryParse(look.Source, out intValue))
                {

                    instructions.Add(new Instruction(OpCode.move, intValue));
                }
                else if (double.TryParse(look.Source, out doubleValue))
                {
                    instructions.Add(new Instruction(OpCode.move, doubleValue));
                }
                else
                {
                    throw (new CompilerException("unable to convert "+ look.Source, line, column));
                }
                Next();
            }
            else
            {
                var symbol = symbols.Find(s => s.Name == look.Source);
                if (symbol == null)
                {
                    throw (new CompilerException("literal or identifier expected", line, column));
                }

                switch (symbol.Type.Name)
                {
                    case SymbolType.CandleSymbol:
                    case SymbolType.CandlesSymbol:
                        Next();
                        if (look.Source == ".")
                        {
                            ClassMember(symbol);
                        }
                        else
                        {
                            Rewind();
                            Identifier(symbol);
                        }
                        break;
                    case SymbolType.ProcSymbol:
                        pos++;
                        CallProcedure(symbol as ProcSymbol);
                        break;
                    case SymbolType.IntSymbol:
                    case SymbolType.StringSymbol:
                    case SymbolType.BoolSymbol:
                    case SymbolType.FloatSymbol:
                    case SymbolType.PaneSymbol:
                    case SymbolType.SeriesSymbol:
                        // pos++;
                        Identifier(symbol);
                        break;
                    default:
                        var @enum = symbol as EnumSymbol;
                        if (@enum == null)
                        {
                            throw (new CompilerException("literal, var, enum or procedure expected", line, column));
                        }
                        else
                        {
                            Next();
                            Match(".");
                            var value = @enum.Values.Find(e => e.Name == look.Source);
                            instructions.Add(new Instruction(OpCode.move, value.Constant));
                            Next();
                        }
                        break;
                }
            }
        }

        public string GetOutput()
        {
            string output = "";
            int address = 0;
            foreach(var instruction in instructions)
            {
                output += string.Format("{0:000}", address) + ": " + instruction.OpCode.ToString() + " ";
                
                if (instruction.Parameter is int)
                {
                    output += (int)instruction.Parameter;
                }
                else if (instruction.Parameter is double)
                {
                    output += (double)instruction.Parameter;
                }
                else if (instruction.Parameter is string)
                {
                    output += (string)instruction.Parameter;
                }
                else if (instruction.Parameter is bool)
                {
                    output += (bool)instruction.Parameter ? "true" : "false";
                }
                else if (instruction.Parameter == null)
                {
                    output += null;
                }
                else if (instruction.Parameter is OpenCompiler.SymbolType)
                {
                    var symbolType = instruction.Parameter as SymbolType;
                    output += "SymbolType " + symbolType.Name;
                }
                else
                {
                    output += "unknown";
                }

                if (!string.IsNullOrEmpty(instruction.Comment))
                {
                    output += " ; "+instruction.Comment;
                }

                output += '\n';
                address++;
            }

            return output;
        }
    }
}

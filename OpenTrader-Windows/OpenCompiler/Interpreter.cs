using ILGPU.IR;
using Microsoft.CodeAnalysis;
using OpenTrader;
using OpenTrader.Indicators;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace OpenCompiler
{
    internal class Interpreter
    {
        List<int> breakpoints = new List<int>() { 1149 };
        List<object> property = new List<object>();
        List<Frame> frameStack = new List<Frame>();
        List<object>? stack;
        List<object> array;
        Frame? frame; // the current frame
        List<int>? calls;
        object register = new object();
        List<Instruction> instructions;
        List<Symbol> symbols;
        OpenScript openScript;

        public Interpreter(List<Instruction> instructions, List<Symbol> symbols) 
        {
            this.instructions = instructions;
            this.symbols = symbols;
        }

        /*
        public object? Property(string symbolName)
        {
            var symbol = symbols.Find(s=>s.Name == symbolName);
            if (symbol == null)  return null;

            Run(symbol.Address);
            if (stack == null || stack.Count == 0) return null;
            return stack[stack.Count - 1];
        }
        */

        public void Execute(OpenScript openScript)
        {
            this.openScript = openScript;
            var symbol = symbols.Find(s => s.Name == "execute");
            if (symbol == null) return;

            Run((symbol as ProcSymbol).Address);
        }

        public void Run(int address=0)
        {
            calls = new List<int>();
            stack = new List<object>();
            if (address == 0)
            {
                frameStack.Clear();
                frameStack.Add(new Frame());
            }
            
            frame = frameStack[0];
            while (address < instructions.Count)
            {
                if (breakpoints.Contains(address))
                {
                    System.Diagnostics.Debug.WriteLine("breakpoint "+address+" reached");
                }
                switch (instructions[address].OpCode)
                {
                    case OpCode.noop:
                        address += 1;
                        break;
                    case OpCode.exit:
                        return;
                    case OpCode.ret: // return from subroutine
                        if (calls.Count == 0)
                            return;
                        else {
                            address = calls[calls.Count - 1] + 1;
                            calls.RemoveAt(calls.Count - 1);
                        }
                        break;
                    case OpCode.bra: // branch absolute
                        address = (int)instructions[address].Parameter;
                        break;
                    case OpCode.bsr: // branch to subroutine
                        calls.Add(address);
                        address = (int)instructions[address].Parameter;
                        break;
                    case OpCode.callext: // call external procedure
                        CallExternal((string)instructions[address].Parameter);
                        // frameStack.RemoveAt(frameStack.Count - 1);
                        // frame = frameStack[frameStack.Count - 1];
                        address += 1;
                        break;
                    case OpCode.alloc: // allocate parameter number of objects to memory
                        frame.Alloc((int)instructions[address].Parameter);
                        address += 1;
                        break;
                    case OpCode.store: // store register in memory of current frame
                        try
                        {
                            frame!.Memory[(int)instructions[address].Parameter] = register;
                        }
                        catch
                        {
                            System.Diagnostics.Debug.WriteLine("");
                        }
                        address += 1;
                        break;
                    case OpCode.load: // load register from memory offset of current frame
                        try
                        {
                            register = frame.Memory[(int)instructions[address].Parameter];
                        }
                        catch
                        {
                            register = 0;
                        }
                        address += 1;
                        break;
                    case OpCode.brtrue: // branch if register is not zero
                        if (register is bool)
                        {
                            var value = (bool) register;
                            if (value)
                            {
                                address = (int)instructions[address].Parameter;
                            }
                            else
                            {
                                address += 1;
                            }
                        }
                        break;
                    case OpCode.brfalse: // branch if register is zero
                        if (register is bool)
                        {
                            var value = (bool)register;
                            if (!value)
                            {
                                address = (int)instructions[address].Parameter;
                            }
                            else
                            {
                                address += 1;
                            }
                        }
                        break;
                    case OpCode.pshr: // push register to stack
                        stack.Add(register);
                        address += 1;
                        break;
                    case OpCode.pop: // pop stack and do nothing with it
                        stack.RemoveAt(stack.Count-1);
                        address += 1;
                        break;
                    case OpCode.popr: // pop the top of the stack and put it in the register
                        register = stack[^1];
                        stack.RemoveAt(stack.Count - 1);
                        address += 1;
                        break;
                    case OpCode.and: // and the stack with the register leaving the result in the register
                        if (register is bool && stack[^1] is bool)
                        {
                            register = (bool)register && (bool)stack[^1];                          
                        }
                        else
                        {
                            register = false;
                        }
                        address += 1;
                        break;
                    case OpCode.or: // or the register with the top of the stack
                        if (register is bool && stack[^1] is bool)
                        {
                            register = (bool)register || (bool)stack[^1];
                        }
                        else
                        {
                            register = false;
                        }
                        address += 1;
                        break;
                    case OpCode.not: // not the register
                        if (register is bool)
                        {
                            register = !(bool)register;
                        }
                        else
                        {
                            register = false;
                        }
                        address += 1;
                        break;
                    case OpCode.cmpr: // compare the stack with the register, leaving the result on the stack
                        var typeOfRegister = register.GetType();
                        var typeOfStackTop = stack[^1].GetType();
                        if (register is int && stack[^1] is double)
                        {
                            var result = ((double)stack[^1]).CompareTo((int)register);
                            stack[^1] = result;
                        }
                        else if (register is int && stack[^1] is int)
                        {
                            var result = ((int)stack[^1]).CompareTo((int)register);
                            stack[^1] = result;
                        }
                        else if (register is double && stack[^1] is double)
                        {
                            var result = ((double)stack[^1]).CompareTo((double)register);
                            stack[^1] = result;
                        }
                        else if (register is string && stack[^1] is string)
                        {
                            var result = ((string)stack[^1]).CompareTo((string)register);
                            stack[^1] = result;
                        }
                        else
                        {
                            stack[^1] = (int)-1;
                        }
                        address += 1;
                        break;
                    case OpCode.seq:
                        if (register is bool)
                        {
                            register = (bool )register == false;
                        }
                        else if (register is int)
                        {
                            register = (int)register == 0;
                        }
                        else if (register is double)
                        {
                            register = (int)register == 0;
                        } else
                        {
                            register = false;
                        }
                        address += 1;
                        break;
                    case OpCode.sne:
                        if (register is bool)
                        {
                            register = (bool)register == true;
                        }
                        else if (register is int)
                        {
                            register = (int)register != 0;
                        }
                        else if (register is double)
                        {
                            register = (int)register != 0;
                        }
                        else
                        {
                            register = true;
                        }
                        address += 1;
                        break;
                    case OpCode.slt:
                        if (register is int)
                        {
                            register = (int)register < 0;
                        }
                        else if (register is double)
                        {
                            register = (int)register < 0;
                        }
                        else
                        {
                            register = false;
                        }
                        address += 1;
                        break;
                    case OpCode.sle:
                        if (register is int)
                        {
                            register = (int)register <= 0;
                        }
                        else if (register is double)
                        {
                            register = (int)register <= 0;
                        }
                        else
                        {
                            register = false;
                        }
                        address += 1;
                        break;
                    case OpCode.sgt:
                        if (register is int)
                        {
                            register = (int)register > 0;
                        }
                        else if (register is double)
                        {
                            register = (int)register > 0;
                        }
                        else
                        {
                            register = false;
                        }
                        address += 1;
                        break;
                    case OpCode.sge:
                        if (register is int)
                        {
                            register = (int)register >= 0;
                        }
                        else if (register is double)
                        {
                            register = (int)register >= 0;
                        }
                        else
                        {
                            register = false;
                        }
                        address += 1;
                        break;
                    case OpCode.move: // move value to register
                        register = instructions[address].Parameter;
                        address += 1; 
                        break;
                    case OpCode.add: // add register to stack, leave result on stack
                        if (register is int && stack[^1] is int)
                        {
                            stack[^1] = (int)register + (int)stack[^1];
                        }
                        else if (register is double && stack[^1] is double)
                        {
                            stack[^1] = (double)register + (double)stack[^1];
                        }
                        else if (register is string && stack[^1] is string)
                        {
                            stack[^1] = (string)register + (string)stack[^1];
                        }
                        else if (register is DataSeries && stack[^1] is DataSeries)
                        {
                            stack[^1] = (DataSeries)register + (DataSeries)stack[^1];
                        }
                        else
                        {
                            stack[^1] = (int)-1;
                        }
                        address += 1;
                        break;
                    case OpCode.sub: // subtract register from stack, leave result on the stack
                        if (register is int && stack[^1] is int)
                        {
                            stack[^1] = (int)register - (int)stack[^1];                        
                        }
                        else if (register is double && stack[^1] is double)
                        {
                            stack[^1] = (double)register - (double)stack[^1];
                        }
                        else if (register is DataSeries && stack[^1] is DataSeries)
                        {
                            stack[^1] = (DataSeries)stack[^1] - (DataSeries)register;
                        }
                        else 
                        {
                            stack[^1] = 0;
                        }
                        address += 1;
                        break;
                    case OpCode.div: // divide stack by register, leave result on the stack
                        if (register is int && stack[^1] is int)
                        {
                            stack[^1] = (double)register / (double)stack[^1]  ;
                        }
                        else if (register is double && stack[^1] is double)
                        {
                            stack[^1] = (double)register / (double)stack[^1]  ;
                        }
                        else if (register is DataSeries && stack[^1] is int)
                        {
                            stack[^1] = (DataSeries )register / (int )stack[^1]  ;
                        }
                        else if (register is DataSeries && stack[^1] is double)
                        {
                            stack[^1] = (DataSeries)register / (double)stack[^1];
                        }
                        else
                        {
                            register = 0;
                        }
                        address += 1;
                        break;
                    case OpCode.mul: // mulitply register by stack, leave result on the stack
                        if (register is int && stack[^1] is int)
                        {
                            stack[^1] = (int)register * (int)stack[^1];
                        }
                        else if (register is double && stack[^1] is double)
                        {
                            stack[^1] = (double)register * (double)stack[^1];
                        }
                        else if (register is int && stack[^1] is DataSeries)
                        {
                            stack[^1] = (int)register * (DataSeries)stack[^1];
                        }
                        else if (register is DataSeries && stack[^1] is int)
                        {
                            stack[^1] = (DataSeries)register * (int)stack[^1];
                        }
                        else
                        {
                            register = 0;
                        }
                        address += 1;
                        break;
                    case OpCode.neg: // negate register
                        if (register is int)
                        {
                            register = - (int)register;
                        }
                        else if (register is double)
                        {
                            register = -(double)register;
                        }
                        else
                        {
                            register = 0;
                        }
                        address += 1;
                        break;
                    case OpCode.exch: // exchange register with top of stack
                        object temp = register;
                        register = stack[^1];
                        stack[^1] = temp;
                        address += 1;
                        break;
                    case OpCode.pushf: // push new frame onto frame stack
                        frameStack.Add( new Frame());
                        address += 1;
                        break;
                    case OpCode.popf: // pop top of frame stack
                        frameStack.RemoveAt(frameStack.Count-1);
                        frame = frameStack[frameStack.Count - 1];
                        address += 1;
                        break;
                    case OpCode.frame: // set the current frame using frameStack with index contained in parameter
                        int index = (int)instructions[address].Parameter;
                        frame = frameStack[frameStack.Count-index-1];
                        address += 1;
                        break;
                    case OpCode.frame0: // set the current frame using frameStack with index contained in parameter
                        index = (int)instructions[address].Parameter;
                        frame = frameStack[index];
                        address += 1;
                        break;
                }
            }
        }


        public void CallExternal(string name)
        {
            switch(name) 
            {
                case "annotateBar":
                    annotateBar();
                    break;
                case "argb":
                    argb();
                    break;
                case "bollingerLower":
                    bollingerLower();
                    break;
                case "bollingerUpper":
                    bollingerUpper();
                    break;
                case "chartType":
                    chartType();
                    break;
                case "candleWidth":
                    candleWidth();
                    break;
                case "createPane":
                    createPane();
                    break;
                case "close":
                    close();
                    break;
                case "description":
                    description();
                    break;
                case "drawLabel":
                    drawLabel();
                    break;
                case "ema":
                    ema();
                    break;
                case "findProfitable":
                    findProfitable();
                    break;
                case "getCandles":
                    getCandles();
                    break;
                case "getCount":
                    getCount();
                    break;
                case "getElement":
                    getElement();
                    break;
                case "getMember":
                    getMember();
                    break;
                case "periodHigh":
                    periodHigh();
                    break;
                case "periodLow":
                    periodLow();
                    break;
                case "plotSeries":
                    plotSeries();
                    break;
                case "plotSeriesFillBand":
                    plotSeriesFillBand();
                    break;
                case "plotSeriesOscillator":
                    plotSeriesOscillator();
                    break;
                case "pricePane":
                    pricePane();
                    break;
                case "print":
                    print();
                    break;
                case "sma":
                    sma();
                    break;
                case "stochastic":
                    stochastic();
                    break;
                case "volume":
                    volume();
                    break;
                case "volumePane":
                    volumePane();
                    break;
                default:
                    break;
            }
        }

        public void annotateBar()
        {
            string text = (string)frame!.Memory[0];
            int bar = (int)frame!.Memory[1];
            bool above = (bool)frame!.Memory[2];
            System.Drawing.Color fontColor = System.Drawing.Color.FromArgb((int)frame!.Memory[3]);
            System.Drawing.Color background = System.Drawing.Color.FromArgb((int)frame!.Memory[4]);

            openScript.AnnotateBar(text, bar, above, fontColor, background);
        }

        public void argb()
        {
            int a = (int)frame!.Memory[0];
            int rgb = (int)frame!.Memory[1];
            int alpha = a << 24;
            int color = alpha | (rgb & 0x00ffffff);
            register = color;
        }



        public void bollingerLower()
        {
            DataSeries series = frame!.Memory[0] as DataSeries;
            int period = (int)frame!.Memory[1];
            double stdDevs = (double)frame!.Memory[2];
            register = OpenTrader.Indicators.BBandLower.Series(series, period, stdDevs);
        }

        public void bollingerUpper()
        {
            DataSeries series = frame!.Memory[0] as DataSeries;
            int period = (int)frame!.Memory[1];
            double stdDevs = (double)frame!.Memory[2];
            register = OpenTrader.Indicators.BBandUpper.Series(series, period, stdDevs);
        }

        public void candleWidth()
        {
            register = (double)openScript.CandleWidth;
        }

        public void chartType()
        {
            register = (int)openScript.ChartType;
        }

        public void close()
        {
            register = openScript.bars.Close;
        }

        public void createPane()
        {
            int size = (int)frame!.Memory[0];
            register = openScript.CreatePane(size);
        }

        public void description()
        { 
            DataSeries series = frame!.Memory[0] as DataSeries;
            register = series.Description;
        }

        public void drawLabel()
        {
            OpenTrader.Pane pane = (OpenTrader.Pane)frame!.Memory[0];
            string label = (string)frame!.Memory[1];
            System.Drawing.Color color = System.Drawing.Color.FromArgb((int)frame!.Memory[2]);
            openScript.DrawLabel(pane, label, color);
        }

        public void findProfitable()
        {
            var candles = (Candles) frame!.Memory[0]!;
            var marketType = (MarketType) frame!.Memory[1]!;
            var n = (int)frame!.Memory[2];
            var ratioType = (RatioType)frame!.Memory[3]!;
            var ratio = (double)frame!.Memory[4]!;
            var startIdx = (int)frame!.Memory[5]!;
            register = candles.FindProfitable(openScript.DataSet,marketType,n,ratioType,ratio,startIdx);
        }

        public void getCandles()
        {
            int smaPeriod = (int) frame!.Memory[0];
            int emaPeriod = (int) frame!.Memory[1];
            register = new Candles(openScript.bars, smaPeriod, emaPeriod);
        }

        public void getCount()
        {
            register = 0;
            if( frame!.Memory[0] is System.Collections.IEnumerable iEnumerable)
            {
                Type type = frame!.Memory[0].GetType();
                PropertyInfo? countProperty = type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                if (countProperty != null && countProperty.PropertyType == typeof(int))
                {
                   register = countProperty.GetValue(frame!.Memory[0], null);
                }
            }            
        }

        public void getElement()
        {
            register = 0;
            if (frame!.Memory[0] is System.Collections.IList iList)
            {
                var index = (int)frame!.Memory[1];
                register = iList[index];
            }
        }

        public void getMember()
        {
            var obj = frame!.Memory[0];
            var type = (string)frame!.Memory[1];
            var member = (string)frame!.Memory[2];
            switch (type) {
                case "Candle":
                    var candle = ((string name, int bar))obj;
                    switch (member)
                    {
                        case "name":
                            register = candle.name;
                            break;
                        case "bar":
                            register = candle.bar;
                            break;
                        default:
                            register = null;
                            break;
                    }
                    break;
                case "Pattern":
                    var pattern = ((string name, int[] bars)) obj;
                    switch (member)
                    {
                        case "name":
                            register = pattern.name;
                            break;
                        case "bars":
                            register = pattern.bars;
                            break;
                        default:
                            register = null;
                            break;
                    }
                    break;
                case "Extrema":
                    var extrema = (Extrema)obj;
                    switch (member)
                    {
                        case "position":
                            register = extrema.position;
                            break;
                        case "extremaType":
                            register = extrema.extremaType;
                            break;
                        default:
                            register = null;
                            break;
                    }
                    break;
            }
        }

        public void ema()
        {
            DataSeries series = frame!.Memory[0] as DataSeries;
            int period = (int)frame!.Memory[1];
            register = OpenTrader.Indicators.EMA.Series(series, period);
        }

        public void makeArray()
        {
            string type = (string) frame!.Memory[0];
            switch (type)
            {
                case "Candle":
                    register = new List<(string name, int bar)>();
                    break;
                case "Pattern":
                    register = new List<(string name, int[] bars)> ();
                    break;
                case "int":
                    register = new List<int>();
                    break;
                case "string":
                    register = new List<string>();
                    break;
                case "bool":
                    register = new List<bool>();
                    break;
                case "float":
                    register = new List<double>();
                    break;
            }
        }

        public void periodHigh()
        {
            DataSeries series = frame!.Memory[0] as DataSeries;
            int period = (int)frame!.Memory[1];
            register = OpenTrader.Indicators.PeriodHigh.Series(series, period);
        }

        public void periodLow()
        {
            DataSeries series = frame!.Memory[0] as DataSeries;
            int period = (int)frame!.Memory[1];
            register = OpenTrader.Indicators.PeriodLow.Series(series, period);
        }

        public void plotSeries()
        {
            OpenTrader.Pane pane = (OpenTrader.Pane) frame!.Memory[0];
            DataSeries dataseries = (DataSeries) frame!.Memory[1];
            System.Drawing.Color color = System.Drawing.Color.FromArgb((int)frame!.Memory[2]);
            LineStyle linestyle = (int)frame!.Memory[3] switch
            {
                0 => LineStyle.Histogram,
                1 => LineStyle.Dashes,
                2 => LineStyle.Solid,
                3 => LineStyle.Dashes,
                _ => LineStyle.Solid
            };
            int thickness = (int)frame!.Memory[4];
            openScript.PlotSeries(pane, dataseries, color, linestyle, thickness);
        }

        public void plotSeriesFillBand()
        {
            OpenTrader.Pane pane = (OpenTrader.Pane)frame!.Memory[0];
            DataSeries ds1 = (DataSeries)frame!.Memory[1];
            DataSeries ds2 = (DataSeries)frame!.Memory[2];
            System.Drawing.Color lineColor = System.Drawing.Color.FromArgb((int)frame!.Memory[3]);
            System.Drawing.Color bandColor = System.Drawing.Color.FromArgb((int)frame!.Memory[4]);
            LineStyle linestyle = (int)frame!.Memory[5] switch
            {
                0 => LineStyle.Histogram,
                1 => LineStyle.Dashes,
                2 => LineStyle.Solid,
                3 => LineStyle.Dashes,
                _ => LineStyle.Solid
            };
            int thickness = (int)frame!.Memory[6];
            openScript.PlotSeriesFillBand(pane, ds1, ds2, lineColor, bandColor, linestyle, thickness);
        }

        public void plotSeriesOscillator()
        {
            OpenTrader.Pane pane = (OpenTrader.Pane)frame!.Memory[0];
            DataSeries source = (DataSeries)frame!.Memory[1];
            Double overbought = (double)frame!.Memory[2];
            Double oversold = (double)frame!.Memory[3];
            System.Drawing.Color overboughtColor = System.Drawing.Color.FromArgb((int)frame!.Memory[4]);
            System.Drawing.Color oversoldColor = System.Drawing.Color.FromArgb((int)frame!.Memory[5]);
            System.Drawing.Color color = System.Drawing.Color.FromArgb((int)frame!.Memory[6]);
            LineStyle style = (int)frame!.Memory[7] switch
            {
                0 => LineStyle.Histogram,
                1 => LineStyle.Dashes,
                2 => LineStyle.Solid,
                3 => LineStyle.Dashes,
                _ => LineStyle.Solid
            };
            int width = (int)frame!.Memory[8];
            openScript.PlotSeriesOscillator(pane, source, overbought, oversold, overboughtColor, oversoldColor, color, style, width);
        }

        public void pricePane()
        {
            register = openScript.PricePane;
        }

        public void print()
        {
            System.Diagnostics.Debug.WriteLine(frame!.Memory[0]);
        }

        public void sma()
        {
            DataSeries series =  frame!.Memory[0] as DataSeries;
            int period = (int)frame!.Memory[1];
            register = OpenTrader.Indicators.SMA.Series(series, period);
        }

        public void stochastic()
        {
            DataSeries series = frame!.Memory[0] as DataSeries;
            int period = (int)frame!.Memory[1];
            register = OpenTrader.Indicators.Stochastic.Series(series, period);
        }

        public void volume()
        {
            register = openScript.bars.Volume;
        }

        public void volumePane()
        {
            register = openScript.VolumePane;
        }

    }
}

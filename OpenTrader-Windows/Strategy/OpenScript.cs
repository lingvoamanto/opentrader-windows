using ILGPU.Frontend;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCompiler;

namespace OpenTrader
{

    internal class OpenScript : TraderScript
    {
        List<Symbol>? symbols;
        List<Instruction>? instructions;

        internal OpenScript(List<Symbol>? symbols, List<Instruction> instructions, ChartType chartType)
        {
            this.chartType = chartType;
            this.symbols = symbols;
            this.instructions = instructions;
        }   

        override public void Execute()
        {
            if (instructions!= null)
            {
                var interpreter = new Interpreter(instructions, symbols);
                interpreter.Run();
                interpreter.Execute(openScript: this);
            }
        }
    }
}


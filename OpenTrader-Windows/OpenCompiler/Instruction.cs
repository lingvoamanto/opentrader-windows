using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace OpenCompiler
{
    internal struct Instruction
    {
        internal OpenCompiler.OpCode OpCode;
        internal string Comment;
        internal object Parameter;

        internal Instruction(OpCode opcode, object parameter)
        {
            OpCode = opcode;
            Parameter = parameter;
            Comment = "";
        }

        internal Instruction(OpCode opcode, object parameter, string comment)
        {
            OpCode = opcode;
            Parameter = parameter;
            Comment = comment;
        }
    }
}

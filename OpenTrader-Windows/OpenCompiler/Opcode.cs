using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCompiler
{
    internal enum OpCode // 26 opcode instruction set
    {
        noop, // do nothing
        exit, // exit the interpreter
        ret, // return from subroutine
        bra, // branch absolute
        bsr, // branch to subroutine
        callext, // call external procedure
        alloc, // allocate parameter number of objects to memory of current frame
        store, // store register in memory of current frame
        load, // load register from memory of current frame
        brtrue, // branch if register is true
        brfalse, // branch if register is false
        pshr, // push register to stack
        pop, // pop stack
        popr, // pop the top of the stack and put it in the register
        and, // and the stack with the register leaving the result in the register
        or, // or the register with the top of the stack
        not, // not the register
        cmpr, // compare the stack with the register, leaving the result in the register
        seq, // set register to true if == 0
        sne, // set register to true if != 0
        slt, // set register to true if < 0
        sgt, // set register to true if > 0
        sle, // set register to true if <= 0
        sge, // set register to true if >= 0
        move, // move value to register
        add, // add register to stack and leave result in register
        sub, // subtract register from stack and leave result in register
        div, // divide stack by register
        mul, // mulitply register by stack
        neg, // negate register
        exch, // exchange register with top of stack
        pushf, // push new frame onto frame stack
        popf, // pop top of frame stack
        frame, // set the current frame using frameStack with index contained in parameter
        frame0, // set the current frame up from the zero frame, will always be zero
        elem, // get register (nth) element of array on stack
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCompiler
{
    internal class Block
    {
        Block? previous;
        int offset;
        int frame;
        Block? next;
        bool isBase = false;
        internal static Block? CurrentBlock;

        internal Block? Previous
        {
            get { return previous; }
        }

        internal Block? Next
        {
            get { return next; }
            set { next = value; }
        }

        internal int Offset
        {
            get { return offset; }
        }

        internal int Frame
        {
            get { return frame; }
            set { frame = value; }
        }

        internal void Add(int size = 1)
        {
            offset += size;
        }


        internal bool IsBase
        {
            get { return isBase; }
            set { isBase = value; }
        }

        internal Block(Block? previous)
        {
            this.previous = previous;
            CurrentBlock = this;
            offset = 0;

            if (previous == null)
            {
                frame = 0;
            }
            else
            {
                frame = previous.Frame + 1;
            }

            if (previous != null)
            {
                previous.Next = this;
            }
        }

        internal void Rewind()
        {
            CurrentBlock = CurrentBlock.previous;
            CurrentBlock.next = null;
        }
    }
}

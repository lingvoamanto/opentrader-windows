using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader.Data
{
    public enum ReadResult
    {
        Success=0,
        NoResponse=1,
        Empty=2,
        NoTBody=3,
        BadMatch=4,
        Unknown =5
    }
}

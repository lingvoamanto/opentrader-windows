using System;
using System.Data;
#if __WINDOWS__
#else
using Mono.Data.Sqlite;
#endif
using System.IO;
using System.Collections.Generic;
using OpenTrader.Data;

namespace OpenTrader
{
    public partial class Preferences
    {
        string mStrategyPath = null;
    }
}




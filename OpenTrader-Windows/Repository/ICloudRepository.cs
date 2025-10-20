using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader
{
    public interface ICloudRepository<T> : IRepository<T> where T : ICloudRepository<T>, new()
    {
        public Guid Guid { get; set; }

    }
}

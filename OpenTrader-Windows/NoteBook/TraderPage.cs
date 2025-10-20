using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader
{
    public class TraderPage : OpenTrader.Components.Page
    {
        internal TraderBook parent;
        internal PageType pageType;

        public virtual void Show()
        {
            parent.ActivePage = pageType;
        }

        public TraderPage(TraderBook parent, PageType pageType) : base(parent)
        {
            this.parent = parent;
            this.pageType = pageType;
        }
    }
}

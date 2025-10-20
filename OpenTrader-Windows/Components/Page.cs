using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenTrader.Components
{

    public class Page : TabItem
    {
        private DockPanel content;

        public UIElementCollection Children
        {
            get
            {
                return content.Children;
            }
        }

        public Page(TabControl parent)
        {
            parent.Items.Add(this);
            content = new DockPanel();
            content.Height = parent.Height;
            base.Content = this.content;
            Tag = parent;
        }
    }
}

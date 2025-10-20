using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenTrader.Components
{
    public class Book : System.Windows.Controls.TabControl
    {
        public Book()
        {
            Tag = this;
        }
        public int CurrentPage
        {
            get
            {
                for ( int i=0; i < this.Items.Count; i++ )
                {
                    if(((Page)Items[i]).IsSelected)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        public int AppendPage(Control control)
        {
            Page page = new Page(this);
            this.Items.Add(page);
            page.Content = control;
            return this.Items.Count - 1;
        }

        public int AppendPage(Page page)
        {
            this.Items.Add(page);
            return this.Items.Count - 1;
        }

        public void RemovePage(int pageNo)
        {
            Items.Remove(pageNo);
        }
    }
}

using OpenTrader.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenTrader.Controls
{
    /// <summary>
    /// Interaction logic for OpenScriptDialog.xaml
    /// </summary>
    public partial class OpenScriptDialog : UserControl
    {
        public OpenScriptDialog()
        {
            InitializeComponent();

            Populate();
        }

        void Populate()
        {
            var scriptFiles = Data.ScriptFile.GetAll();
            ScriptList.Items.Clear();

            foreach (var scriptFile in scriptFiles)
            {
                var scriptItem = new ScriptItem();
                scriptItem.ScriptFile = scriptFile;

                ScriptList.Items.Add(scriptItem);
            }
        }
    }
}

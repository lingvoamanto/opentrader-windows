using OpenTrader.Data;
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

namespace OpenTrader.Item
{
    /// <summary>
    /// Interaction logic for ScriptItem.xaml
    /// </summary>
    public partial class ScriptItem : UserControl
    {
        ScriptFile scriptFile;

        public ScriptFile ScriptFile
        {
            get => scriptFile;
            set {
                scriptFile = value;
                NameText.Text = scriptFile.Name;
                ModifiedText.Text = scriptFile.Modified.ToString();
                LanguageText.Text = scriptFile.Language switch
                {
                    OpenTrader.Language.OpenScript => "OpenScript",
                    OpenTrader.Language.CSharp => "C#",
                    OpenTrader.Language.FSharp => "F#"
                };
            }
        }

        public ScriptItem()
        {
            InitializeComponent();
        }
    }
}

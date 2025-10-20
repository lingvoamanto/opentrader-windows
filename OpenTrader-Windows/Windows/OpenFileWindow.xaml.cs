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
using System.Windows.Shapes;

namespace OpenTrader.Windows
{
    /// <summary>
    /// Interaction logic for OpenFileWindow.xaml
    /// </summary>
    public partial class OpenFileWindow : Window
    {
        public SaveDelegate OnSave;
        public OpenFileWindow()
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

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OnSave?.Invoke(ScriptList.SelectedItem);
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

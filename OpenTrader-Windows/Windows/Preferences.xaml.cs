using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
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
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class PreferencesWindow : Window
    {

        public PreferencesWindow()
        {
            InitializeComponent();

            OpenTraderUser.Text = MainWindow.OpenTraderUser.Value;
            OpenTraderPassword.Password = MainWindow.OpenTraderPassword.Value;
            SharesiesUser.Text = MainWindow.SharesiesUser.Value;
            SharesiesPassword.Password = MainWindow.SharesiesPassword.Value;
            StrategyPath.Text = MainWindow.StrategyPath.Value;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = false;

            MainWindow.SharesiesUser.Value = SharesiesUser.Text;
            MainWindow.SharesiesUser.Save();
            MainWindow.SharesiesPassword.Value = SharesiesPassword.Password;
            MainWindow.SharesiesPassword.Save();
            MainWindow.OpenTraderUser.Value = OpenTraderUser.Text;
            MainWindow.OpenTraderUser.Save();
            MainWindow.OpenTraderPassword.Value = OpenTraderPassword.Password;
            MainWindow.OpenTraderPassword.Save();
        }

        private void StrategyPath_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog()
            {
                SelectedPath = MainWindow.StrategyPath.Value
            };

            fbd.ShowDialog();
            if (! string.IsNullOrEmpty( fbd.SelectedPath))
            {
                MainWindow.StrategyPath.Value = fbd.SelectedPath;
                MainWindow.StrategyPath.Save();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace OpenTrader
{
    /// <summary>
    /// Interaction logic for WeekWindow.xaml
    /// </summary>
    public partial class WeekWindow : Window
    {
        Controls.ChartControl chartControl;
        public WeekWindow(Controls.ChartControl chartControl)
        {
            InitializeComponent();
            chartControl.Margin = new Thickness(0, 0, 0, 10);
            ChartParent.Children.Add(chartControl);
            this.chartControl = chartControl;
            this.SizeChanged += WeekWindow_SizeChanged;  
        }

        private void WeekWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            chartControl.UpdateChartLayout();
            chartControl.UpdateScrollbar();
        }
    }
}

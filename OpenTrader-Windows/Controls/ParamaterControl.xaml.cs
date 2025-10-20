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
    /// Interaction logic for ParameterControl.xaml
    /// </summary>
    public partial class ParameterControl : UserControl
    {
        StrategyParameter strategyParameter;
        public delegate void ValueChangedDelegate();
        public ValueChangedDelegate ValueChanged;

        public TraderBook TraderBook { get; set; }
        public ParameterControl(StrategyParameter strategyParameter)
        {
            InitializeComponent();
            this.strategyParameter = strategyParameter;
            SpinButton.Step = strategyParameter.Step;
            SpinButton.Minimum = strategyParameter.Start;
            SpinButton.Maximum = strategyParameter.Stop;
            SpinButton.Value = strategyParameter.Value;
            // strategyParameter.OnValueChanged += Parameter_ValueChanged;
            Label.Content = strategyParameter.Name;
            SpinButton.ValueChanged += SpinButton_ValueChanged;
        }

        public StrategyParameter StrategyParameter
        {
            get => strategyParameter;
            set
            {
                strategyParameter = value;
                // strategyParameter.OnValueChanged += Parameter_ValueChanged;
                Label.Content = strategyParameter.Name;
                SpinButton.ValueChanged += SpinButton_ValueChanged;
            }
        }

        private void SpinButton_ValueChanged(object oSpinButton, EventArgs e)
        {
            var spinButton = oSpinButton as Controls.Spinner;
            strategyParameter.Value = SpinButton.Value;
            ValueChanged?.Invoke();
        }

  


        public static explicit operator StackPanel(ParameterControl control)
        {
            return control.StackPanel;
        }

        public double Value
        {
            set => SpinButton.Value = value; 
            get => SpinButton.Value; 
        }
    }
}

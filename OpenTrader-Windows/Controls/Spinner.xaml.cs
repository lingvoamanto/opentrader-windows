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
    /// Interaction logic for Spinner.xaml
    /// </summary>
    /// 

    public partial class Spinner : UserControl
    {
        public event ChangedEventHandler ValueChanged;
        public double Step { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        double value;

        public Spinner()
        {
            InitializeComponent();

            Step = 1.0;
            Value = 0;
            Minimum = double.MinValue;
            Maximum = double.MaxValue;
            this.Increment.Click += Increment_Click;
            this.Decrement.Click += Decrement_Click;
        }

        void Decrement_Click(object sender, RoutedEventArgs e)
        {
            value -= Step;
            if (value < Minimum)
                value = Minimum;
            this.TextBox.Text = value.ToString();
            ValueChanged?.Invoke(this, e);
        }

        void Increment_Click(object sender, RoutedEventArgs e)
        {
            value += Step;
            if (value > Maximum)
                value = Maximum;
            this.TextBox.Text = value.ToString();
            ValueChanged?.Invoke(this, e);
        }

        private void OnValueChanged(EventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }

        public double Value
        {
            get { return value;  }
            set
            {
                this.value = value;
                if (this.value < Minimum)
                    this.value = Minimum;
                if (this.value > Maximum)
                    this.value = Maximum;
                this.TextBox.Text = this.value.ToString();
                OnValueChanged(EventArgs.Empty);
            }
        }
    }
}

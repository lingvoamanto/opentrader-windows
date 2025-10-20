using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader.NeuralNetwork
{
    public class StepActivationFunction : IActivationFunction
    {
        private double _threshold;

        public StepActivationFunction(double threshold)
        {
            _threshold = threshold;
        }

        public double CalculateOutput(double input)
        {
            return Convert.ToDouble(input > _threshold);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader.NeuralNetwork
{
    public class SigmoidActivationFunction : IActivationFunction
    {
        private double _coefficient;

        public SigmoidActivationFunction(double coefficient)
        {
            _coefficient = coefficient;
        }

        public SigmoidActivationFunction()
        {
            _coefficient = 1.0;
        }

        public double CalculateOutput(double input)
        {
            return (1 / (1 + Math.Exp(-input * _coefficient)));
        }

        // The derivative of the sigmoid function σ(x) is the sigmoid function σ(x) multiplied by 1−σ(x).
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader.NeuralNetwork
{
    public class WeightedSumFunction : IInputFunction
    {
        /// <summary>
        /// eturn some sort of value based on the data contained in the list of connections. 
        /// </summary>
        /// <param name="inputs">List of connections that are described in the ISynapse interface</param>
        /// <returns>Weighted sum of the data contained in the list of connections (synapses)</returns>
        public double CalculateInput(List<ISynapse> inputs, double bias)
        {
            double sum = 0;

            foreach (var input in inputs)
            {
                sum += input.Weight * input.GetOutput();
            }

            sum += bias;

            return sum;
        }
    }
}

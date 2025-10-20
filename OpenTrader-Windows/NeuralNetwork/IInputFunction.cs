using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader.NeuralNetwork
{
    public interface IInputFunction
    {
        double CalculateInput(List<ISynapse> inputs, double bias);
    }
}

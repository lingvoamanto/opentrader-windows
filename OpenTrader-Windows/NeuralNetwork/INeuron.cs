using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader.NeuralNetwork
{
    /// <summary>
    /// The workflow that a neuron should follow goes like this: 
    /// Receive input values from one or more weighted input connections. 
    /// Collect those values and pass them to the activation function, which calculates the output value of the neuron. 
    /// Send those values to the outputs of the neuron. 
    /// </summary>
    public interface INeuron
    {
        Guid Id { get; } // Each neuron has its unique identifier – Id. This property is used in the backpropagation algorithm later.
        double PreviousPartialDerivate { get; set; }

        /// <summary>
        /// Input connections of the neuron.
        /// </summary>
        List<ISynapse> Inputs { get; set; }

        double Bias { get; set; }
        /// <summary>
        /// Output connections of the neuron.
        /// </summary>
        List<ISynapse> Outputs { get; set; }

        /// <summary>
        /// Connect two neurons. 
        /// This neuron is the output neuron of the incoming connection.
        /// </summary>
        /// <param name="inputNeuron">Neuron that will be input neuron of the newly created connection.</param>
        void AddInputNeuron(INeuron inputNeuron);

        /// <summary>
        /// Connect two neurons. 
        /// This neuron is the input neuron of the connection.
        /// </summary>
        /// <param name="outputNeuron">Neuron that will be output neuron of the newly created connection.</param>

        void AddOutputNeuron(INeuron inputNeuron);

        /// <summary>
        /// Calculate output value of the neuron.
        /// </summary>
        /// <returns>
        /// Output of the neuron.
        /// </returns>
        double CalculateOutput();

        /// <summary>
        /// Input Layer neurons just receive input values.
        /// For this they need to have connections.
        /// This function adds this kind of connection to the neuron.
        /// </summary>
        /// <param name="inputValue">
        /// Initial value that will be "pushed" as an input to connection.
        /// </param>
        void AddInputSynapse(double inputValue);

        /// <summary>
        /// Sets new value on the input connections.
        /// </summary>
        /// <param name="inputValue">
        /// New value that will be "pushed" as an input to connection.
        /// </param>
        void PushValueOnInput(double inputValue);
    }
}

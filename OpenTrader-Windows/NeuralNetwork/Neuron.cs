using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader.NeuralNetwork
{
    public class Neuron : INeuron
    {
        private IActivationFunction _activationFunction;
        private IInputFunction _inputFunction;

        /// <summary>
        /// Input connections of the neuron.
        /// </summary>
        public List<ISynapse> Inputs { get; set; }

        public double Bias { get; set; }

        /// <summary>
        /// Output connections of the neuron.
        /// </summary>
        public List<ISynapse> Outputs { get; set; }

        public Guid Id { get; set; }

        /// <summary>
        /// Calculated partial derivate in previous iteration of training process.
        /// </summary>
        /// <param name="inputFunction">Sums all weighted inputs that are active on input connections – the weighted input function</param>
        /// <param name="activationFunction">Receives a value from the input function and according to this value, it generates an output value and propagates them to the outputs</param>
        public double PreviousPartialDerivate { get; set; }

        public Neuron(IActivationFunction activationFunction, IInputFunction inputFunction)
        {
            Id = Guid.NewGuid();
            Inputs = new List<ISynapse>();
            Outputs = new List<ISynapse>();

            _activationFunction = activationFunction;
            _inputFunction = inputFunction;
        }

        /// <summary>
        /// Connect two neurons. 
        /// This neuron is the output neuron of the connection.
        /// </summary>
        /// <param name="inputNeuron">Neuron that will be input neuron of the newly created connection.</param>
        public void AddInputNeuron(INeuron inputNeuron)
        {
            var synapse = new Synapse(inputNeuron, this);
            var tempRand = new Random();
            Bias = tempRand.NextDouble();
            Inputs.Add(synapse);
            inputNeuron.Outputs.Add(synapse);
        }

        /// <summary>
        /// Connect two neurons. 
        /// This neuron is the input neuron of the connection.
        /// </summary>
        /// <param name="outputNeuron">Neuron that will be output neuron of the newly created connection.</param>
        public void AddOutputNeuron(INeuron outputNeuron)
        {
            var synapse = new Synapse(this, outputNeuron);
            Outputs.Add(synapse);
            outputNeuron.Inputs.Add(synapse);
        }

        /// <summary>
        /// Calculate output value of the neuron.
        /// </summary>
        /// <returns>
        /// Output of the neuron.
        /// </returns>
        public double CalculateOutput()
        {
            return _activationFunction.CalculateOutput(_inputFunction.CalculateInput(this.Inputs, this.Bias));
        }

        /// <summary>
        /// Input Layer neurons just receive input values.
        /// For this they need to have connections.
        /// This function adds this kind of connection to the neuron.
        /// </summary>
        /// <param name="inputValue">
        /// Initial value that will be "pushed" as an input to connection.
        /// </param>
        public void AddInputSynapse(double inputValue)
        {
            var inputSynapse = new InputSynapse(this, inputValue);
            Inputs.Add(inputSynapse);
        }

        /// <summary>
        /// Sets new value on the input connections.
        /// </summary>
        /// <param name="inputValue">
        /// New value that will be "pushed" as an input to connection.
        /// </param>
        public void PushValueOnInput(double inputValue)
        {
            ((InputSynapse)Inputs.First()).Output = inputValue;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader.NeuralNetwork
{
    public class SimpleNeuralNetwork
    {
        private NeuralLayerFactory _layerFactory;

        internal List<NeuralLayer> _layers;
        internal Dictionary<int, double[]> _neuronErrors;
        internal double _learningRate;
        internal double[][] _expectedResult;

        /// <summary>
        /// Constructor of the Neural Network.
        /// Note:
        /// Initialy input layer with defined number of inputs will be created.
        /// </summary>
        /// <param name="numberOfInputNeurons">
        /// Number of neurons in input layer.
        /// </param>
        public SimpleNeuralNetwork(int numberOfInputNeurons)
        {
            _layers = new List<NeuralLayer>();
            _neuronErrors = new Dictionary<int, double[]>();
            _layerFactory = new NeuralLayerFactory();

            // Create input layer that will collect inputs.
            CreateInputLayer(numberOfInputNeurons);

            _learningRate = .01;
        }

        /// <summary>
        /// Add layer to the neural network.
        /// Layer will automatically be added as the output layer to the last layer in the neural network.
        /// </summary>
        public void AddLayer(NeuralLayer newLayer)
        {
            if (_layers.Any())
            {
                var lastLayer = _layers.Last();
                newLayer.ConnectLayers(lastLayer);
            }

            _layers.Add(newLayer);
            _neuronErrors.Add(_layers.Count - 1, new double[newLayer.Neurons.Count]);
        }

        /// <summary>
        /// Push input values to the neural network.
        /// </summary>
        public void PushInputValues(double[] inputs)
        {
            var neurons = _layers.First().Neurons;
            neurons.ForEach(x => x.PushValueOnInput(inputs[neurons.IndexOf(x)]));
        }

        /// <summary>
        /// Set expected values for the outputs.
        /// </summary>
        public void PushExpectedValues(double[][] expectedOutputs)
        {
            _expectedResult = expectedOutputs;
        }

        /// <summary>
        /// Calculate output of the neural network.
        /// </summary>
        /// <returns></returns>
        public List<double> GetOutput()
        {
            var returnValue = new List<double>();
            var neurons = _layers.Last().Neurons;
            foreach (var neuron in neurons)
            {
                var output = neuron.CalculateOutput();
                returnValue.Add(output);
            }

            return returnValue;
        }

        /// <summary>
        /// Train neural network.
        /// </summary>
        /// <param name="inputs">Input values</param>
        /// <param name="numberOfEpochs">The frequency at which training data passes through the algorithm.  It helps to identify the model that represents the data as faithfully as possible.</param>
        public void Train(double[][] inputs, int numberOfEpochs)
        {
            double totalError = 0;

            for (int i = 0; i < numberOfEpochs; i++)
            {
                for (int j = 0; j < inputs.GetLength(0); j++)
                {
                    var inputValues = inputs[j];
                    PushInputValues(inputValues);

                    var outputs = new List<double>();

                    // Get outputs.
                    var neurons = _layers.Last().Neurons;
                    foreach (var neuron in neurons)
                    {
                        var output = neuron.CalculateOutput();
                        outputs.Add(output);
                    }

                    // Calculate error by summing errors on all output neurons.
                    totalError = CalculateTotalError(outputs, j);
                    HandleOutputLayer(j);
                    HandleHiddenLayers();
                }
            }
        }

        /// <summary>
        /// Helper function that creates input layer of the neural network.
        /// </summary>
        private void CreateInputLayer(int numberOfInputNeurons)
        {
            var inputLayer = _layerFactory.CreateNeuralLayer(numberOfInputNeurons, new RectifiedActivationFunction(), new WeightedSumFunction());
            inputLayer.Neurons.ForEach(x => x.AddInputSynapse(0));
            this.AddLayer(inputLayer);
        }

        /// <summary>
        /// Helper function that calculates total error of the neural network.
        /// </summary>
        private double CalculateTotalError(List<double> outputs, int row)
        {
            double totalError = 0;

            outputs.ForEach(output =>
            {
                var error = Math.Pow(output - _expectedResult[row][outputs.IndexOf(output)], 2);
                totalError += error;
            });

            return totalError;
        }

        /// <summary>
        /// Helper function that runs backpropagation algorithm on the output layer of the network.
        /// </summary>
        /// <param name="row">
        /// Input/Expected output row.
        /// </param>
        private void HandleOutputLayer(int row)
        {
            var neurons = _layers.Last().Neurons;
            foreach (var neuron in neurons)
            {
                var inputs = neuron.Inputs;
                foreach (var connection in inputs)
                {
                    var output = neuron.CalculateOutput();
                    var netInput = connection.GetOutput();

                    var expectedOutput = _expectedResult[row][neurons.IndexOf(neuron)];

                    // // The derivative of the sigmoid function σ(x) is the sigmoid function σ(x) multiplied by 1−σ(x).
                    var sigmoidDerivative = output * (1 - output);
                    // // The derivative of the cost function σ(expect-actual)^2 is 2 * (expected C
                    var costDerivative = 2 * (expectedOutput - output);
                    var nodeDelta = costDerivative * sigmoidDerivative;
                    var delta = -1 * netInput * nodeDelta;

                    connection.UpdateWeight(_learningRate, delta);

                    neuron.PreviousPartialDerivate = nodeDelta;
                }

            }
        }

        /// <summary>
        /// Helper function that runs backpropagation algorithm on the hidden layer of the network.
        /// </summary>
        /// <param name="row">
        /// Input/Expected output row.
        /// </param>
        private void HandleHiddenLayers()
        {
            for (int k = _layers.Count - 2; k > 0; k--)
            {
                _layers[k].Neurons.ForEach(neuron =>
                {
                    neuron.Inputs.ForEach(connection =>
                    {
                        var output = neuron.CalculateOutput();
                        var netInput = connection.GetOutput();
                        double sumPartial = 0;

                        _layers[k + 1].Neurons
                        .ForEach(outputNeuron =>
                        {
                            outputNeuron.Inputs.Where(i => i.IsFromNeuron(neuron.Id))
                            .ToList()
                            .ForEach(outConnection =>
                            {
                                sumPartial += outConnection.PreviousWeight * outputNeuron.PreviousPartialDerivate;
                            });
                        });

                        var delta = -1 * netInput * sumPartial * output * (1 - output);
                        connection.UpdateWeight(_learningRate, delta);
                    });
                });
            }
        }
    }
}

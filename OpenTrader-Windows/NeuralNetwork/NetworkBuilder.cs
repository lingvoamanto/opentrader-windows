using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTrader.NeuralNetwork
{
    public class NetworkBuilder
    {
        private readonly int _inputCount;
        private readonly NeuralLayerFactory _layerFactory;
        private readonly SimpleNeuralNetwork _network;

        public NetworkBuilder(int inputCount)
        {
            _inputCount = inputCount;
            _layerFactory = new NeuralLayerFactory();
            _network = new SimpleNeuralNetwork(_inputCount);
        }

        public NetworkBuilder AddSigmoidOutput(double gain = 1.0)
        {
            var outputLayer = _layerFactory.CreateNeuralLayer(
                1,
                new SigmoidActivationFunction(gain),
                new WeightedSumFunction()
            );
            _network.AddLayer(outputLayer);
            return this;
        }

        public NetworkBuilder AddRectifiedHidden(int count)
        {
            var hiddenLayer = _layerFactory.CreateNeuralLayer(
                count,
                new RectifiedActivationFunction(),
                new WeightedSumFunction()
            );
            _network.AddLayer(hiddenLayer);
            return this;
        }

        public SimpleNeuralNetwork Build() => _network;
    }
}

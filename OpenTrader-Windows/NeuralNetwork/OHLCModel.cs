using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Effects;
using Accord.IO;
using Microsoft.VisualBasic.Devices;
using OpenTrader.Indicators;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Markup;
using System;

namespace OpenTrader.NeuralNetwork
{
    public class OHLCMOdel
    {
        Dictionary<string, INeuron> neuronDictionary = new();

        class NeuronJson
        {
            public string Id { get; set; }
            public double Bias { get; set; }
            public List<SynapseJson> Synapses { get; set; }
        }

        public class SynapseJson
        {
            public double Weight { get; set; }
            public double PreviousWeight { get; set; }
            public double Bias { get; set; }

            public string FromNeuron { get; set; }

            public string ToNeuron { get; set; }
        }

        SimpleNeuralNetwork model = Create();

        public static SimpleNeuralNetwork Create()
        {
            return new NetworkBuilder(120)
                .AddRectifiedHidden(11)
                .AddRectifiedHidden(2)
                .AddSigmoidOutput()
                .Build();
        }

        string Serialise()
        {
            var jsonLayers = new List<List<NeuronJson>>();

            foreach (var layer in model._layers)
            {
                var jsonLayer = new List<NeuronJson>();

                foreach (Neuron neuron in layer.Neurons)
                {
                    var jsonNeuron = new NeuronJson
                    {
                        Id = neuron.Id.ToString(),
                        Bias = neuron.Bias,
                        Synapses = neuron.Inputs.Select(s =>
                        {
                            var synapse = s as Synapse;
                            return new SynapseJson
                            {
                                Weight = synapse.Weight,
                                PreviousWeight = synapse.PreviousWeight,
                                Bias = synapse.Bias,
                                FromNeuron = synapse.FromNeuron.Id.ToString(),
                                ToNeuron = synapse.ToNeuron.Id.ToString()
                            };
                        }).ToList()
                    };

                    jsonLayer.Add(jsonNeuron);
                }

                jsonLayers.Add(jsonLayer);
            }

            return JsonConvert.SerializeObject(jsonLayers, Formatting.Indented);
        }

        void Deserialise(string json)
        {
            neuronDictionary.Clear();
            var jsonLayers = JsonConvert.DeserializeObject<List<List<NeuronJson>>>(json);
            int layerIndex = 0;


            foreach (NeuralLayer neuralLayer in model._layers)
            {
                var jsonLayer = jsonLayers[layerIndex];
                int neuronIndex = 0;

                foreach (Neuron neuron in neuralLayer.Neurons)
                {
                    var jsonNeuron = jsonLayer[neuronIndex];
                    neuron.Bias = jsonNeuron.Bias;
                    neuron.Id = new Guid(jsonNeuron.Id);
                    neuronDictionary[jsonNeuron.Id] = neuron;

                    for (int synapseIndex = 0; synapseIndex < neuron.Inputs.Count; synapseIndex++)
                    {
                        var jsonSynapse = jsonNeuron.Synapses[synapseIndex];
                        var synapse = neuron.Inputs[synapseIndex];

                        synapse.Weight = jsonSynapse.Weight;
                        synapse.PreviousWeight = jsonSynapse.PreviousWeight;
                        synapse.Bias = jsonSynapse.Bias;
                    }

                    neuronIndex++;
                }

                layerIndex++;
            }


            layerIndex = 0;

            foreach (NeuralLayer neuralLayer in model._layers)
            {
                var jsonLayer = jsonLayers[layerIndex];
                int neuronIndex = 0;

                foreach (Neuron neuron in neuralLayer.Neurons)
                {
                    var jsonNeuron = jsonLayer[neuronIndex];

                    for (int synapseIndex = 0; synapseIndex < neuron.Inputs.Count; synapseIndex++)
                    {
                        var jsonSynapse = jsonNeuron.Synapses[synapseIndex];
                        var synapse = neuron.Inputs[synapseIndex];

                        (synapse as Synapse).FromNeuron = neuronDictionary[jsonSynapse.FromNeuron];
                        (synapse as Synapse).ToNeuron = neuronDictionary[jsonSynapse.ToNeuron];
                    }

                    neuronIndex++;
                }

                layerIndex++;
            }
        }


        public void Train(DataSeries open, DataSeries high, DataSeries low, DataSeries close)
        {
            int windowSize = 30;
            int count = close.Count - windowSize;
            var longSMA = SMA.Series(close, 399);
            var shortSMA = SMA.Series(close, 41);

            var inputVectors = new List<double[]>();
            var labels = new List<double[]>();

            for (int i = 399; i < count; i++)
            {
                double[] input = new double[windowSize * 4]; // open, high, low, close
                for (int j = 0; j < windowSize; j++)
                {
                    input[j * 4 + 0] = open[i + j];
                    input[j * 4 + 1] = high[i + j];
                    input[j * 4 + 2] = low[i + j];
                    input[j * 4 + 3] = close[i + j];
                }

                inputVectors.Add(input);

                double futureClose = close[i + windowSize];
                double entryPrice = close[i + windowSize - 1];
                double netProfit = futureClose - entryPrice; // You could subtract cost here
                double label = netProfit > entryPrice * 0.15 ? 1.0 : 0.0;

                labels.Add(new double[] { label });
            }

            model.PushExpectedValues(labels.ToArray());
            model.Train(inputVectors.ToArray(), numberOfEpochs: count-399);
        }

        public double Predict(DataSeries open, DataSeries high, DataSeries low, DataSeries close, int startIndex)
        {
            double[] input = new double[30 * 4];
            for (int j = 0; j < 30; j++)
            {
                input[j * 4 + 0] = open[startIndex + j];
                input[j * 4 + 1] = high[startIndex + j];
                input[j * 4 + 2] = low[startIndex + j];
                input[j * 4 + 3] = close[startIndex + j];
            }

            model.PushInputValues(input);
            var outputs = model.GetOutput();
            return outputs[0];
        }
    }
}

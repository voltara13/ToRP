using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using MathNet.Numerics.Distributions;

namespace ToRP.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private const string FileName = "generate.csv";

        private readonly Func<double, double, double> _spacingFunction =
            (intensity, randomValue) => -1 / intensity * Math.Log(randomValue);

        private double _expectedValue = 10.0;

        private double _intensity = 0.4;

        private double _mean;
        private double _simulationTime = 100.0;
        private double _standardDeviation = 4.0;

        private int _countExperiments = 1;

        public int ErrorsCount;

        public RelayCommand GenerateCommand;
        private SeriesCollection _seriesCollection = new();

        public MainWindowViewModel()
        {
            GenerateCommand = new RelayCommand(OnGenerateCommandExecute, OnGenerateCommandCanExecute);

            OnGenerate();
        }

        public SeriesCollection SeriesCollection
        {
            get => _seriesCollection;
            set => OnSeriesCollectionChanged(SeriesCollection, value);
        }

        public double Intensity
        {
            get => _intensity;
            set => OnIntensityChanged(Intensity, value);
        }

        public double ExpectedValue
        {
            get => _expectedValue;
            set => OnExpectedValueChanged(ExpectedValue, value);
        }

        public double StandardDeviation
        {
            get => _standardDeviation;
            set => OnStandardDeviationChanged(StandardDeviation, value);
        }

        public double SimulationTime
        {
            get => _simulationTime;
            set => OnSimulationTimeChanged(SimulationTime, value);
        }

        public int CountExperiments
        {
            get => _countExperiments;
            set => OnCountExperimentsChanged(CountExperiments, value);
        }

        public double Mean
        {
            get => _mean;
            set => OnMeanChanged(Mean, value);
        }

        public string Error { get; }

        public string this[string columnName]
        {
            get
            {
                var result = string.Empty;

                switch (columnName)
                {
                    case nameof(Intensity) when Intensity < 0:
                    case nameof(ExpectedValue) when ExpectedValue < 0:
                    case nameof(StandardDeviation) when StandardDeviation < 0:
                    case nameof(SimulationTime) when SimulationTime < 0:
                    case nameof(CountExperiments) when CountExperiments < 0:
                    {
                        result = "¬еличина не может быть отрицательной";
                        break;
                    }
                }

                return result;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool OnGenerateCommandCanExecute(object arg)
        {
            return string.IsNullOrEmpty(Error) && ErrorsCount == 0;
        }

        private void OnGenerateCommandExecute(object obj)
        {
            OnGenerate();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnSeriesCollectionChanged(SeriesCollection oldValue, SeriesCollection newValue)
        {
            if (Equals(oldValue, newValue))
            {
                return;
            }

            _seriesCollection = newValue;

            OnPropertyChanged(nameof(SeriesCollection));
        }

        private void OnIntensityChanged(double oldValue, double newValue)
        {
            if (Math.Abs(oldValue - newValue) < 1e-4)
            {
                return;
            }

            _intensity = newValue;

            OnPropertyChanged(nameof(Intensity));
        }

        private void OnExpectedValueChanged(double oldValue, double newValue)
        {
            if (Math.Abs(oldValue - newValue) < 1e-4)
            {
                return;
            }

            _expectedValue = newValue;

            OnPropertyChanged(nameof(ExpectedValue));
        }

        private void OnStandardDeviationChanged(double oldValue, double newValue)
        {
            if (Math.Abs(oldValue - newValue) < 1e-4)
            {
                return;
            }

            _standardDeviation = newValue;

            OnPropertyChanged(nameof(StandardDeviation));
        }

        private void OnCountExperimentsChanged(int oldValue, int newValue)
        {
            if (oldValue == newValue)
            {
                return;
            }

            _countExperiments = newValue;

            OnPropertyChanged(nameof(CountExperiments));
        }

        private void OnSimulationTimeChanged(double oldValue, double newValue)
        {
            if (Math.Abs(oldValue - newValue) < 1e-4)
            {
                return;
            }

            _simulationTime = newValue;

            OnPropertyChanged(nameof(SimulationTime));
        }

        private void OnMeanChanged(double oldValue, double newValue)
        {
            if (Math.Abs(oldValue - newValue) < 1e-4)
            {
                return;
            }

            _mean = newValue;

            OnPropertyChanged(nameof(Mean));
        }


        private void OnAddSeries(ChartValues<KeyValuePair<double, double>> values, string title)
        {
            var item = new LineSeries
            {
                Values = values,
                LineSmoothness = 0,
                Title = title,
                Fill = Brushes.Transparent,
                PointGeometrySize = 5,
                Configuration = new CartesianMapper<KeyValuePair<double, double>>()
                    .X(point => point.Key)
                    .Y(point => point.Value)
            };

            _seriesCollection.Add(item);
        }

        public async void OnGenerate()
        {
            _seriesCollection.Clear();

            var generatorCount = new Normal(_expectedValue, _standardDeviation);
            var generatorTime = new ContinuousUniform(0, 1);

            _mean = 0.0;

            await using (var writer = new StreamWriter(FileName, false))
            {
                for (var i = 0; i < _countExperiments; ++i)
                {
                    await writer.WriteLineAsync($"EXPERIMENT {i + 1}");

                    var currentTime = _spacingFunction(_intensity, generatorTime.Sample());

                    var chartValue = new ChartValues<KeyValuePair<double, double>> {new(0.0, 0.0)};

                    var sumGeneratorValue = 0.0;

                    var countEvents = 0;

                    while (currentTime < _simulationTime)
                    {
                        var generatorValue = 0.0;

                        while (generatorValue < 1e-4)
                        {
                            generatorValue = Math.Round(generatorCount.Sample());
                        }

                        chartValue.Add(new KeyValuePair<double, double>(currentTime, generatorValue));

                        await writer.WriteLineAsync($"{currentTime};{generatorValue}");

                        currentTime += _spacingFunction(_intensity, generatorTime.Sample());

                        sumGeneratorValue += generatorValue;

                        countEvents++;
                    }

                    _mean += countEvents != 0 ? sumGeneratorValue / countEvents : 0;

                    OnAddSeries(chartValue, $"Ёксперимент {i + 1}");
                }
            }

            _mean = Math.Round(_countExperiments != 0 ? _mean / _countExperiments : 0.0, 2);

            OnPropertyChanged(nameof(SeriesCollection));
            OnPropertyChanged(nameof(Mean));
        }
    }
}
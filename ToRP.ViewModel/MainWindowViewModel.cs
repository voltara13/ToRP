using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
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
        private readonly Func<double, double, double> _spacingFunction = (intensity, randomValue) => -1 / intensity * Math.Log(randomValue);

        private double _expectedValue = 10.0;
        private double _intensity = 0.4;
        private double _simulationTime = 100.0;
        private double _standardDeviation = 4.0;

        private GenerateCommand? _generateCommand;

        public int ErrorsCount;
        private SeriesCollection _seriesCollection = new();

        public MainWindowViewModel()
        {
            OnGenerateGraphs();
        }

        public Func<double, string> LabelFormatter => value => Math.Round(value, 2).ToString();

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

        public GenerateCommand GenerateCommand
        {
            get
            {
                return _generateCommand ??= new GenerateCommand(_ => OnGenerateGraphs(),
                    _ => string.IsNullOrEmpty(Error) && ErrorsCount == 0);
            }
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
                    {
                        result = "Величина не может быть отрицательной";
                        break;
                    }
                }

                return result;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

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

        private void OnSimulationTimeChanged(double oldValue, double newValue)
        {
            if (Math.Abs(oldValue - newValue) < 1e-4)
            {
                return;
            }

            _simulationTime = newValue;

            OnPropertyChanged(nameof(SimulationTime));
        }


        private void OnAddSeries(ChartValues<KeyValuePair<double, int>> values, string title)
        {
            var item = new LineSeries
            {
                Values = values,
                LineSmoothness = 0,
                Title = title,
                Fill = Brushes.Transparent,
                PointGeometrySize = 5,
                Configuration = new CartesianMapper<KeyValuePair<double, int>>()
                    .X(point => point.Key)
                    .Y(point => point.Value)
            };

            SeriesCollection.Add(item);
        }

        public void OnGenerateGraphs()
        {
            SeriesCollection.Clear();

            var generatorCount = new Normal(ExpectedValue, StandardDeviation);
            var generatorTime = new ContinuousUniform(0, 1);

            var currentTime = _spacingFunction(Intensity, generatorTime.Sample());

            var values = new ChartValues<KeyValuePair<double, int>>();

            while (currentTime < SimulationTime)
            {
                var count = (int) Math.Round(Math.Abs(generatorCount.Sample()));

                values.Add(new KeyValuePair<double, int>(currentTime, count));

                currentTime += _spacingFunction(Intensity, generatorTime.Sample());
            }

            OnAddSeries(values, "Кол-во вагонов");

            OnPropertyChanged(nameof(SeriesCollection));
        }
    }
}
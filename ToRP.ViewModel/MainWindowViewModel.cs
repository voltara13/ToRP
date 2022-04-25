using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using MathNet.Numerics.Distributions;

namespace ToRP.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly FunctionDelegate _function = (arguments) =>
        {
            var U = arguments[0];
            var V = arguments[1];
            var w = arguments[2];
            var t = arguments[3];

            return U * Math.Cos(w * t) + V * Math.Sin(w * t);
        };

        private readonly FunctionDelegate _standardDeviation = (arguments) =>
        {
            var w = arguments[0];
            var t = arguments[1];

            return Math.Sqrt(0.25 * (1 - 0.7 * (Math.Sin(2 * t * w))));
        };

        private readonly FunctionDelegate _expected = (_) => 0;

        private delegate double FunctionDelegate(params double[] arguments);

        private double _constantValue = 3.0;
        private double _expectedValueU;
        private double _expectedValueV;
        private double _standardDeviationU = 0.5;
        private double _standardDeviationV = 0.5;

        private GenerateCommand? _generateCommand;
        private int _countPoints = 10;
        private int _countTests = 10;

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

        public int CountPoints
        {
            get => _countPoints;
            set => OnCountPointsChanged(CountPoints, value);
        }

        public int CountTests
        {
            get => _countTests;
            set => OnCountTestsChanged(CountTests, value);
        }

        public double ExpectedValueU
        {
            get => _expectedValueU;
            set => OnExpectedValueUChanged(ExpectedValueU, value);
        }

        public double ExpectedValueV
        {
            get => _expectedValueV;
            set => OnExpectedValueVChanged(ExpectedValueV, value);
        }

        public double StandardDeviationU
        {
            get => _standardDeviationU;
            set => OnStandardDeviationUChanged(StandardDeviationU, value);
        }

        public double StandardDeviationV
        {
            get => _standardDeviationV;
            set => OnStandardDeviationVChanged(StandardDeviationV, value);
        }

        public double ConstantValue
        {
            get => _constantValue;
            set => OnConstantValueChanged(ConstantValue, value);
        }

        public GenerateCommand GenerateCommand
        {
            get
            {
                return _generateCommand ??= new GenerateCommand(_ => OnGenerateGraphs(), _ => string.IsNullOrEmpty(Error) && ErrorsCount == 0);
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
                    case nameof(CountTests) when CountTests < 0:
                    case nameof(CountPoints) when CountPoints < 0:
                    {
                        result = "Количество не может быть отрицательным";
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

        private void OnCountPointsChanged(int oldValue, int newValue)
        {
            if (oldValue == newValue)
            {
                return;
            }

            _countPoints = newValue;

            OnPropertyChanged(nameof(CountPoints));
        }

        private void OnCountTestsChanged(int oldValue, int newValue)
        {
            if (oldValue == newValue)
            {
                return;
            }

            _countTests = newValue;

            OnPropertyChanged(nameof(CountTests));
        }

        private void OnExpectedValueUChanged(double oldValue, double newValue)
        {
            if (Math.Abs(oldValue - newValue) < 1e-4)
            {
                return;
            }

            _expectedValueU = newValue;

            OnPropertyChanged(nameof(ExpectedValueU));
        }

        private void OnExpectedValueVChanged(double oldValue, double newValue)
        {
            if (Math.Abs(oldValue - newValue) < 1e-4)
            {
                return;
            }

            _expectedValueV = newValue;

            OnPropertyChanged(nameof(ExpectedValueV));
        }

        private void OnStandardDeviationUChanged(double oldValue, double newValue)
        {
            if (Math.Abs(oldValue - newValue) < 1e-4)
            {
                return;
            }

            _standardDeviationU = newValue;

            OnPropertyChanged(nameof(StandardDeviationU));
        }

        private void OnStandardDeviationVChanged(double oldValue, double newValue)
        {
            if (Math.Abs(oldValue - newValue) < 1e-4)
            {
                return;
            }

            _standardDeviationV = newValue;

            OnPropertyChanged(nameof(StandardDeviationV));
        }

        private void OnConstantValueChanged(double oldValue, double newValue)
        {
            if (Math.Abs(oldValue - newValue) < 1e-4)
            {
                return;
            }

            _constantValue = newValue;

            OnPropertyChanged(nameof(ConstantValue));
        }

        private void OnAddSeries<T>(IEnumerable<T> values, string title, double strokeThickness, Brush? color = null)
        {
            var item = new LineSeries
            {
                Values = new ChartValues<T>(values),
                LineSmoothness = 0,
                Title = title,
                Fill = Brushes.Transparent,
                StrokeThickness = strokeThickness,
                PointGeometrySize = 5
            };

            if (color != null)
            {
                item.Stroke = color;
            }

            SeriesCollection.Add(item);
        }

        private IEnumerable<double> OnGetChartValues(FunctionDelegate functionDelegate, params Func<double, double>[] arguments)
        {

            for (var t = 0; t < CountPoints; ++t)
            {
                var tempArguments = arguments.Select(x => x(0)).ToList();
                tempArguments.Add(t);

                yield return functionDelegate(tempArguments.ToArray());
            }
        }

        public void OnGenerateGraphs()
        {
            SeriesCollection.Clear();

            var generatorU = new Normal(ExpectedValueU, StandardDeviationU);
            var generatorV = new Normal(ExpectedValueV, StandardDeviationV);

            for (var i = 0; i < CountTests; ++i)
            {
                OnAddSeries(OnGetChartValues(_function, _ => generatorU.Sample(), _ => generatorV.Sample(), _ => ConstantValue), $"Процесс {i + 1}", 1);
            }

            OnAddSeries(OnGetChartValues(_standardDeviation, _ => ConstantValue), "Ср. кв. откл.", 2, Brushes.Black);
            OnAddSeries(OnGetChartValues(_expected), "Мат. ож.", 2, Brushes.Brown);

            OnPropertyChanged(nameof(SeriesCollection));
        }
    }
}
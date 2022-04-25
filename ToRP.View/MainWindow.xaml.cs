using System.Windows;
using System.Windows.Controls;
using ToRP.ViewModel;

namespace ToRP.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel();
        }

        private void OnValidationError(object sender, ValidationErrorEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                if (e.Action == ValidationErrorEventAction.Added)
                {
                    viewModel.ErrorsCount++;
                }
                else
                {
                    viewModel.ErrorsCount--;
                }
            }
        }
    }
}

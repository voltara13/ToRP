using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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

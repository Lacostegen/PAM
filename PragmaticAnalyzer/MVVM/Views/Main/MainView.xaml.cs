using PragmaticAnalyzer.MVVM.ViewModel.Main;
using PragmaticAnalyzer.Services;
using System.ComponentModel;
using System.Windows;

namespace PragmaticAnalyzer.MVVM.Views.Main
{
    public partial class MainView
    {
        private readonly MainViewModel _vm;

        public MainView(MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            var workArea = SystemParameters.WorkArea;
            MaxWidth = workArea.Width;
            MaxHeight = workArea.Height;
            Width = Math.Min(1280, workArea.Width);
            Height = Math.Min(720, workArea.Height);
            Left = workArea.Left + Math.Max(0, (workArea.Width - Width) / 2);
            Top = workArea.Top + Math.Max(0, (workArea.Height - Height) / 2);
        }

        private async void OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;

            try
            {
                InfrastructureOrchestrator.ApiService.StopServer();
                await _vm.OnCompletionWork();
            }
            finally
            {
                Closing -= OnClosing;
                Close();
            }
        }
    }
}

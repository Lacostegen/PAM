using PragmaticAnalyzer.MVVM.ViewModel.Main;
using PragmaticAnalyzer.Services;
using System.ComponentModel;

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
            Closing += OnClosing;
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
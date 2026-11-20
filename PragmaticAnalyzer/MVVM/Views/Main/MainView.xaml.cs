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
            Closing += OnClosing;

            var workArea = SystemParameters.WorkArea;
            MinWidth = Math.Min(MinWidth, workArea.Width);
            MinHeight = Math.Min(MinHeight, workArea.Height);
            Width = Math.Min(1280, workArea.Width);
            Height = Math.Min(720, workArea.Height);
        }

        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers != System.Windows.Input.ModifierKeys.Control) return;
            ShellZoom.ScaleX = ShellZoom.ScaleY = Math.Clamp(
                Math.Round(ShellZoom.ScaleX + Math.Sign(e.Delta) * 0.1, 1), 0.6, 1.5);
            e.Handled = true;
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

using PragmaticAnalyzer.MVVM.ViewModel.Main;

namespace PragmaticAnalyzer.MVVM.Views
{
    public partial class SettingSearchView
    {
        public SettingSearchView(ConnectionViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
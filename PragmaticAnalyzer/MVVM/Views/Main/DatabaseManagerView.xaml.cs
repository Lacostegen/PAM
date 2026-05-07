using PragmaticAnalyzer.MVVM.ViewModel.Main;

namespace PragmaticAnalyzer.MVVM.Views.Main
{
    public partial class DatabaseManagerView
    {
        public DatabaseManagerView(CreatorViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            Closed += (s, e) =>
            {
                vm.CompleteDatabaseManager();
            };   
        }
    }
}
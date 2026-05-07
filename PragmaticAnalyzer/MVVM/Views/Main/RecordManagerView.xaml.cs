using PragmaticAnalyzer.MVVM.ViewModel.Main;

namespace PragmaticAnalyzer.MVVM.Views.Main
{

    public partial class RecordManagerView
    {
        public RecordManagerView(CreatorViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            Closed += (s, e) =>
            {
                vm.CompleteRecordManager(); 
            };
        }
    }
}
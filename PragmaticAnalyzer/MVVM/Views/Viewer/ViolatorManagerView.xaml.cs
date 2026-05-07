using PragmaticAnalyzer.MVVM.ViewModel.Viewer;

namespace PragmaticAnalyzer.MVVM.Views.Viewer
{
    public partial class ViolatorManagerView
    {
        public ViolatorManagerView(ViolatorViewModel vm )
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
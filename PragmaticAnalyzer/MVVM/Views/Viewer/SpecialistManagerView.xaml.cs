using PragmaticAnalyzer.MVVM.ViewModel.Viewer;

namespace PragmaticAnalyzer.MVVM.Views.Viewer
{
    public partial class SpecialistManagerView
    {
        public SpecialistManagerView(SpecialistViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
using PragmaticAnalyzer.MVVM.ViewModel.Main;

namespace PragmaticAnalyzer.MVVM.Views.Main
{
    public partial class OntologyManagerView
    {
        public OntologyManagerView(OntologyViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.MVVM.ViewModel.Viewer;
using System.Windows;
using System.Windows.Controls;

namespace PragmaticAnalyzer.MVVM.Views.Viewer
{
    public partial class TacticView : UserControl
    {
        public TacticView()
        {
            InitializeComponent();
        }

        private void SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var viewModel = DataContext as TacticViewModel;
            if (e.NewValue is Technique technique)
            {
                viewModel.SelectedItem = technique;
            }
            else if (e.NewValue is Tactic tactic)
            {
                viewModel.SelectedItem = tactic;
            }
        }
    }
}
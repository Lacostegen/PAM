using System.Windows;
using System.Windows.Controls;

namespace PragmaticAnalyzer.MVVM.Views.Main
{
    public partial class DatabaseLocalSearchBarView
    {
        public DatabaseLocalSearchBarView()
        {
            InitializeComponent();
        }

        private void SearchModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { ContextMenu: not null } button)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}

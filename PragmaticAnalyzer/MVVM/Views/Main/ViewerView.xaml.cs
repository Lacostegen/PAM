using System.Windows;
using System.Windows.Controls;

namespace PragmaticAnalyzer.MVVM.Views.Main
{
    public partial class ViewerView
    {
        public ViewerView()
        {
            InitializeComponent();
        }

        private void DatabaseSearchModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { ContextMenu: not null } button)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}

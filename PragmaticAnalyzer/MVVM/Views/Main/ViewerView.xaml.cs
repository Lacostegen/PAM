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

        private double _zoom = 1;

        private void Records_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Categories == null) return;
            var narrow = e.NewSize.Width < 900;
            DockPanel.SetDock(Categories, narrow ? Dock.Top : Dock.Left);
            Categories.Width = narrow ? double.NaN : 154;
            Categories.Height = narrow ? 70 : double.NaN;
        }

        private void Diagram_SizeChanged(object sender, SizeChangedEventArgs e) => ResizeDiagram();

        private void ResizeDiagram()
        {
            if (DiagramBox == null) return;
            var fit = Math.Min(DiagramArea.ActualWidth / 1248, DiagramArea.ActualHeight / 652);
            DiagramBox.Width = 1248 * fit * _zoom;
            DiagramBox.Height = 652 * fit * _zoom;
        }

        private void Diagram_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            DiagramZoom.AtPoint(DiagramScroll, DiagramBox.Child, e.GetPosition(DiagramScroll), () =>
            {
                _zoom = Math.Clamp(_zoom * Math.Pow(1.15, Math.Sign(e.Delta)), 0.5, 4);
                ResizeDiagram();
            });
            e.Handled = true;
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

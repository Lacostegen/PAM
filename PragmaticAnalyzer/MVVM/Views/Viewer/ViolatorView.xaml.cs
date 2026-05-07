using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PragmaticAnalyzer.MVVM.Views.Viewer
{
    public partial class ViolatorView : UserControl
    {
        private const double MinSchemeScale = 0.35;
        private const double MaxSchemeScale = 3.0;
        private const double SchemeScaleStep = 0.1;

        private double _schemeScale = 1.0;
        private bool _isSchemeVisible;

        public ViolatorView()
        {
            InitializeComponent();
        }

        private void ToggleSchemeButton_Click(object sender, RoutedEventArgs e)
        {
            SetSchemeVisible(!_isSchemeVisible);
        }

        private void HideSchemeButton_Click(object sender, RoutedEventArgs e)
        {
            SetSchemeVisible(false);
        }

        private void SetSchemeVisible(bool isVisible)
        {
            _isSchemeVisible = isVisible;

            ViolatorCardsPanel.Visibility = isVisible
                ? Visibility.Collapsed
                : Visibility.Visible;

            ViolatorSchemePanel.Visibility = isVisible
                ? Visibility.Visible
                : Visibility.Collapsed;

            SchemeToggleButton.Content = isVisible
                ? "Карточка нарушителя"
                : "Схема модели нарушителей";
        }

        private void SchemeScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                ChangeSchemeScale(SchemeScaleStep);
            }
            else
            {
                ChangeSchemeScale(-SchemeScaleStep);
            }

            e.Handled = true;
        }

        private void ResetSchemeZoomButton_Click(object sender, RoutedEventArgs e)
        {
            _schemeScale = 1.0;
            ApplySchemeScale();
        }

        private void ChangeSchemeScale(double delta)
        {
            _schemeScale += delta;

            if (_schemeScale < MinSchemeScale)
            {
                _schemeScale = MinSchemeScale;
            }

            if (_schemeScale > MaxSchemeScale)
            {
                _schemeScale = MaxSchemeScale;
            }

            ApplySchemeScale();
        }

        private void ApplySchemeScale()
        {
            SchemeScaleTransform.ScaleX = _schemeScale;
            SchemeScaleTransform.ScaleY = _schemeScale;
        }

        private void SchemeNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            var nodeName = button.Tag?.ToString() ?? "Неизвестный блок";

            MessageBox.Show(
                $"Нажат блок схемы: {nodeName}",
                "Схема модели нарушителей",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
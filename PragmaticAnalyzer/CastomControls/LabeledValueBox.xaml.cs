using System.Windows;

namespace PragmaticAnalyzer.CastomControls
{
    public partial class LabeledValueBox
    {
        public static readonly DependencyProperty LabelTextProperty =
               DependencyProperty.Register("LabelText", typeof(string), typeof(LabeledValueBox), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueTextProperty =
            DependencyProperty.Register("ValueText", typeof(string), typeof(LabeledValueBox), new PropertyMetadata(string.Empty));

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        public string ValueText
        {
            get => (string)GetValue(ValueTextProperty);
            set => SetValue(ValueTextProperty, value);
        }

        public LabeledValueBox()
        {
            InitializeComponent();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ValueText))
            {
                Clipboard.SetText(ValueText);
            }
        }
    }
}
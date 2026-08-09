using System.Collections;
using System.Windows;

namespace PragmaticAnalyzer.CastomControls
{
    public partial class LabeledValueBox
    {
        public static readonly DependencyProperty LabelTextProperty =
               DependencyProperty.Register("LabelText", typeof(string), typeof(LabeledValueBox), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueTextProperty =
            DependencyProperty.Register("ValueText", typeof(string), typeof(LabeledValueBox), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty HighlightedValueSegmentsProperty =
            DependencyProperty.Register(
                nameof(HighlightedValueSegments),
                typeof(IEnumerable),
                typeof(LabeledValueBox),
                new PropertyMetadata(null));

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

        public IEnumerable? HighlightedValueSegments
        {
            get => (IEnumerable?)GetValue(HighlightedValueSegmentsProperty);
            set => SetValue(HighlightedValueSegmentsProperty, value);
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

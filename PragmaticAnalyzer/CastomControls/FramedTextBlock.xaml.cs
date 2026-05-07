using System.Windows;

namespace PragmaticAnalyzer.CastomControls
{
    public partial class FramedTextBlock
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
                             "Text", typeof(string), typeof(FramedTextBlock), new PropertyMetadata(string.Empty));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public FramedTextBlock()
        {
            InitializeComponent();
        }
    }
}
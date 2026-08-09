using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using PragmaticAnalyzer.MVVM.ViewModel.Main;

namespace PragmaticAnalyzer.CastomControls
{
    public partial class HighlightedTextBlock
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(string.Empty, OnTextContentChanged));

        public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
            nameof(Segments),
            typeof(IEnumerable),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(null, OnTextContentChanged));

        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
            nameof(TextWrapping),
            typeof(TextWrapping),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(TextWrapping.Wrap));

        private static readonly Brush HighlightBrush = CreateHighlightBrush();

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public IEnumerable? Segments
        {
            get => (IEnumerable?)GetValue(SegmentsProperty);
            set => SetValue(SegmentsProperty, value);
        }

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public HighlightedTextBlock()
        {
            InitializeComponent();
            UpdateInlines();
        }

        private static Brush CreateHighlightBrush()
        {
            var brush = new SolidColorBrush(Color.FromRgb(255, 242, 128));
            if (brush.CanFreeze)
            {
                brush.Freeze();
            }

            return brush;
        }

        private static void OnTextContentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((HighlightedTextBlock)dependencyObject).UpdateInlines();
        }

        private void UpdateInlines()
        {
            if (HighlightedText == null)
            {
                return;
            }

            HighlightedText.Inlines.Clear();

            var hasSegments = false;
            if (Segments != null)
            {
                foreach (var segment in Segments)
                {
                    if (segment is HighlightedTextSegment highlightedSegment)
                    {
                        AddRun(highlightedSegment.Text, highlightedSegment.IsMatch);
                        hasSegments = true;
                    }
                }
            }

            if (!hasSegments)
            {
                AddRun(Text ?? string.Empty, false);
            }
        }

        private void AddRun(string text, bool isMatch)
        {
            var run = new Run(text);
            if (isMatch)
            {
                run.Background = HighlightBrush;
                run.FontWeight = FontWeights.Bold;
            }

            HighlightedText.Inlines.Add(run);
        }
    }
}

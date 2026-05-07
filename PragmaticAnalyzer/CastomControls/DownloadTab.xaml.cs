using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PragmaticAnalyzer.CastomControls
{
    public partial class DownloadTab
    {
        public static readonly DependencyProperty ButtonBackgroundProperty =
             DependencyProperty.Register("ButtonBackground", typeof(Brush), typeof(DownloadTab), new PropertyMetadata(null));

        public static readonly DependencyProperty TitleProperty =
              DependencyProperty.Register("Title", typeof(string), typeof(DownloadTab), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register("Time", typeof(string), typeof(DownloadTab), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(DownloadTab), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty RefreshCommandProperty =
            DependencyProperty.Register("RefreshCommand", typeof(ICommand), typeof(DownloadTab), new PropertyMetadata(null));

        public static readonly DependencyProperty RefreshCommandParameterProperty =
            DependencyProperty.Register("RefreshCommandParameter", typeof(object), typeof(DownloadTab), new PropertyMetadata(null));

        public string ButtonBackground
        {
            get => (string)GetValue(ButtonBackgroundProperty);
            set => SetValue(ButtonBackgroundProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Time
        {
            get => (string)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public ICommand RefreshCommand
        {
            get => (System.Windows.Input.ICommand)GetValue(RefreshCommandProperty);
            set => SetValue(RefreshCommandProperty, value);
        }

        public object RefreshCommandParameter
        {
            get => GetValue(RefreshCommandParameterProperty);
            set => SetValue(RefreshCommandParameterProperty, value);
        }

        public DownloadTab()
        {
            InitializeComponent();
        }
    }
}
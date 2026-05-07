using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PragmaticAnalyzer.CastomControls
{
    public partial class ExtendedDownloadTab
    {
        public static readonly DependencyProperty ButtonBackgroundProperty =
            DependencyProperty.Register("ButtonBackground", typeof(Brush), typeof(ExtendedDownloadTab), new PropertyMetadata(null));

        public static readonly DependencyProperty TitleProperty =
              DependencyProperty.Register("Title", typeof(string), typeof(ExtendedDownloadTab), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register("Time", typeof(string), typeof(ExtendedDownloadTab), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ExtendedDownloadTab), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty RefreshUpdateCommandProperty =
            DependencyProperty.Register("RefreshUpdateCommand", typeof(ICommand), typeof(ExtendedDownloadTab), new PropertyMetadata(null));

        public static readonly DependencyProperty RefreshLoadCommandProperty =
            DependencyProperty.Register("RefreshLoadCommand", typeof(ICommand), typeof(ExtendedDownloadTab), new PropertyMetadata(null));

        public static readonly DependencyProperty RefreshCommandParameterProperty =
           DependencyProperty.Register("RefreshCommandParameter", typeof(object), typeof(ExtendedDownloadTab), new PropertyMetadata(string.Empty));

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

        public ICommand RefreshUpdateCommand
        {
            get => (ICommand)GetValue(RefreshUpdateCommandProperty);
            set => SetValue(RefreshUpdateCommandProperty, value);
        }

        public ICommand RefreshLoadCommand
        {
            get => (ICommand)GetValue(RefreshLoadCommandProperty);
            set => SetValue(RefreshLoadCommandProperty, value);
        }

        public object RefreshCommandParameter
        {
            get => GetValue(RefreshCommandParameterProperty);
            set => SetValue(RefreshCommandParameterProperty, value);
        }

        public ExtendedDownloadTab()
        {
            InitializeComponent();
        }
    }
}
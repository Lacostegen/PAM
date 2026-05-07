using PragmaticAnalyzer.MVVM.ViewModel.Main;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PragmaticAnalyzer.MVVM.Views.Main
{
    public partial class CommunicationView
    {
        private bool _isUserScrolling = false;

        public CommunicationView()
        {
            InitializeComponent();
            Loaded += ChatView_Loaded;
        }

        private void ChatView_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollToBottom();
        }

        private void MessagesScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            _isUserScrolling = e.ExtentHeightChange == 0;
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                if (DataContext is CommunicationViewModel vm && vm.SendCommand.CanExecute(null))
                {
                    vm.SendCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void ScrollToBottom()
        {
            if (!_isUserScrolling && MessagesScroll != null)
            {
                MessagesScroll.ScrollToEnd();
            }
        }

        private void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Dispatcher.InvokeAsync(ScrollToBottom);
            }
        }
    }
}
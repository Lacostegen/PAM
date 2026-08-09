using PragmaticAnalyzer.MVVM.ViewModel.Main;
using System.Collections.Specialized;
using System.ComponentModel;
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
            DataContextChanged += CommunicationView_DataContextChanged;
        }

        private void ChatView_Loaded(object sender, RoutedEventArgs e)
        {
            AttachMessagesHandlers();
            ScrollToBottom();
        }

        private void CommunicationView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is CommunicationViewModel oldVm)
            {
                oldVm.Messages.CollectionChanged -= Messages_CollectionChanged;

                foreach (var message in oldVm.Messages)
                {
                    message.PropertyChanged -= Message_PropertyChanged;
                }
            }

            AttachMessagesHandlers();
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
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is INotifyPropertyChanged oldMessage)
                    {
                        oldMessage.PropertyChanged -= Message_PropertyChanged;
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is INotifyPropertyChanged newMessage)
                    {
                        newMessage.PropertyChanged -= Message_PropertyChanged;
                        newMessage.PropertyChanged += Message_PropertyChanged;
                    }
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Dispatcher.InvokeAsync(ScrollToBottom);
            }
        }

        private void Message_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChatMessage.Text))
            {
                Dispatcher.InvokeAsync(ScrollToBottom);
            }
        }

        private void AttachMessagesHandlers()
        {
            if (DataContext is not CommunicationViewModel vm)
            {
                return;
            }

            vm.Messages.CollectionChanged -= Messages_CollectionChanged;
            vm.Messages.CollectionChanged += Messages_CollectionChanged;

            foreach (var message in vm.Messages)
            {
                message.PropertyChanged -= Message_PropertyChanged;
                message.PropertyChanged += Message_PropertyChanged;
            }
        }
    }
}

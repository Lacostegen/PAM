using PragmaticAnalyzer.Databases;
using System.Windows.Controls;
using System.Windows.Input;

namespace PragmaticAnalyzer.MVVM.Views.Viewer
{
    public partial class ProtectionMeasureManagerView
    {
        private Action<ProtectionMeasure> Add;
        private Action<ProtectionMeasure> Change;

        public ProtectionMeasureManagerView(Action<ProtectionMeasure> add, Action<ProtectionMeasure> change, bool addOrChange, ProtectionMeasure? protectionMeasure = null)
        {
            InitializeComponent();
            Add = add;
            Change = change;

            if (!addOrChange && protectionMeasure is not null)
            {      
                for (int i = 0; i < protectionMeasure.SecurityClasses.Length; i++)
                {
                    if (protectionMeasure.SecurityClasses[i] == '1')
                    {
                        ProtectionClassOneCheckBox.IsChecked = true;
                    }
                    if (protectionMeasure.SecurityClasses[i] == '2')
                    {
                        ProtectionClassTwoCheckBox.IsChecked = true;
                    }
                    if (protectionMeasure.SecurityClasses[i] == '3')
                    {
                        ProtectionClassThreeCheckBox.IsChecked = true;
                    }
                }
                NameGroupMeasure.Text = protectionMeasure.NameGroup;
                NameMeasureTextBox.Text = protectionMeasure.Name;
                NumberMeasureTextBox.Text = protectionMeasure.Number;
                DescriptionMeasure.Text = protectionMeasure.FullName;
            }

            DoneButton.Click += (s, e) =>
            { 
                string securityClasses = string.Empty;
                if ((bool)ProtectionClassOneCheckBox.IsChecked)
                    securityClasses += "1";
                if ((bool)ProtectionClassTwoCheckBox.IsChecked)
                    securityClasses += " 2";
                if ((bool)ProtectionClassThreeCheckBox.IsChecked)
                    securityClasses += " 3";

                ProtectionMeasure protectionMeasure = new()
                {
                    NameGroup = NameGroupMeasure.Text,
                    Name = NameMeasureTextBox.Text,
                    Number = NumberMeasureTextBox.Text,
                    FullName = DescriptionMeasure.Text,
                    SecurityClasses = securityClasses,
                };

                if (addOrChange)
                {
                    Add?.Invoke(protectionMeasure);
                }
                else
                {
                    Change?.Invoke(protectionMeasure);
                }
                Close();
            };

            CancelButton.Click += (s, e) =>
            {
                Close();
            };
        }

        private void NameMeasureTextBoxPreviewKeyDown(object s, KeyEventArgs e)
        {
            var tb = (TextBox)s;
            if (e.Key != Key.Back && (e.Key < Key.A || e.Key > Key.Z) || (e.Key != Key.Back && tb.Text.Length == 3))
                e.Handled = true;
        }

        private void NumberMeasureTextBoxPreviewKeyDown(object s, KeyEventArgs e)
        {
            var tb = (TextBox)s;
            if (e.Key != Key.Back && (e.Key < Key.D0 || e.Key > Key.D9) || (e.Key != Key.Back && tb.Text.Length == 2))
                e.Handled = true;
        }
    }
}
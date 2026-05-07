using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.MVVM.ViewModel.Viewer;
using System.Windows;

namespace PragmaticAnalyzer.MVVM.Views.Viewer
{
    public partial class TacticManagerView
    {
        private readonly TacticViewModel viewModel;
        private IEntityTit? SelectedItemComboBox { get; set; }

        public TacticManagerView(Action<IEntityTit, string> add, Action<IEntityTit> change, bool addOrChange, TacticViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            viewModel = vm;

            IEntityTit modifiedElement = vm.SelectedItem;

            if (addOrChange)
            {
                NumberTextBlock.Text = (vm.Tactics.Count + 1).ToString();
            }
            else if (!addOrChange)
            {
                if (modifiedElement is Technique)
                {
                    NumberTextBlock.Text = modifiedElement.Name[1].ToString();
                    SubNumberTextBlock.Text = modifiedElement.Name[3].ToString();
                }
                else
                {
                    NumberTextBlock.Text = modifiedElement.Name[1].ToString();
                }
                DescriptionTextBox.Text = modifiedElement.Description;
                OptionCheckBox.IsEnabled = false;
                ListTechniqueComboBox.IsEnabled = false;
            }

            DoneButton.Click += (s, e) =>
            {
                if (addOrChange)
                {
                    if (OptionCheckBox.IsChecked == true && ListTechniqueComboBox.SelectedItem is not null)
                    {
                        Technique technique = new()
                        {
                            Name = 'Т' + NumberTextBlock.Text + '.' + SubNumberTextBlock.Text,
                            Description = DescriptionTextBox.Text
                        };
                        add?.Invoke(technique, SelectedItemComboBox.Name);
                    }
                    else
                    {
                        Tactic tactic = new()
                        {
                            Name = 'Т' + NumberTextBlock.Text,
                            Description = DescriptionTextBox.Text
                        };
                        add?.Invoke(tactic, "");
                    }
                }
                else if (!addOrChange)
                {
                    if (modifiedElement is Technique)
                    {
                        modifiedElement.Name = 'Т' + NumberTextBlock.Text + '.' + SubNumberTextBlock.Text;
                    }
                    else if (modifiedElement is Tactic)
                    {
                        modifiedElement.Name = 'Т' + NumberTextBlock.Text;
                    }
                    modifiedElement.Description = DescriptionTextBox.Text;
                    change?.Invoke(modifiedElement);
                }
                Close();
            };
        }

        private void ListTechniqueComboBoxSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ListTechniqueComboBox.SelectedItem is null)
                return;

            SelectedItemComboBox = (IEntityTit)ListTechniqueComboBox.SelectedItem;
            foreach (var tactic in viewModel.Tactics)
            {
                if (tactic.Name == SelectedItemComboBox.Name)
                {
                    if(SelectedItemComboBox.Name.Length == 3)
                    {
                        NumberTextBlock.Text = SelectedItemComboBox.Name[1].ToString() + SelectedItemComboBox.Name[2].ToString();
                    }
                    else
                    {
                        NumberTextBlock.Text = SelectedItemComboBox.Name[1].ToString();
                    }                    
                    SubNumberTextBlock.Text = (tactic.Techniques.Count + 1).ToString();
                }
            }
        }
        private void OptionCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            ListTechniqueComboBox.IsEnabled = true;
        }
        private void OptionCheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            ListTechniqueComboBox.IsEnabled = false;
            ListTechniqueComboBox.SelectedItem = null;
            SubNumberTextBlock.Text = null;
            NumberTextBlock.Text = (viewModel.Tactics.Count + 1).ToString();
        }
    }
}
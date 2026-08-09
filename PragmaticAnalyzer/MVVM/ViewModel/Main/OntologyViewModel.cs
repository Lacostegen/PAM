using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.MVVM.Views.Main;
using PragmaticAnalyzer.Services;
using System.Collections.ObjectModel;

namespace PragmaticAnalyzer.MVVM.ViewModel.Main
{
    public class OntologyViewModel : ViewModelBase
    {
        private readonly IFileService _fileService; 
        private OntologyManagerView? _manager;
        public ObservableCollection<Ontology> Ontologys { get; set; } // коллекция онтологий
        public IObjectOntology? SelectedItem { get => Get<IObjectOntology>(); set => Set(value); } // выбранная онтология или сущность
        public string EnteredName { get => Get<string>(); set => Set(value); } // введенное наименование сущности в менеджере
        public string EnteredDescription { get => Get<string>(); set => Set(value); } // введенное описание сущности в менеджере
        public bool IsAdd { get => Get<bool>(); set => Set(value); } // true если необходимо добавить элемент, false если удалить
        public bool AddNewEntity { get => Get<bool>(); set => Set(value); } // true если CheckBox на OntologyManagerView выбран и false если не выбран
        public bool IsEnabledCheckBox { get => Get<bool>(); set => Set(value); } // регулировка свойством IsEnabled у CheckBox на OntologyManagerView

        public OntologyViewModel(ObservableCollection<Ontology> ontologys)
        {  
            Ontologys = ontologys;
            _fileService = new FileService();
            IsEnabledCheckBox = false;
        }

        public RelayCommand LoadCommand => GetCommand(async o =>
        {
            var path = DialogService.OpenFileDialog(DialogService.JsonFilter);
            if (string.IsNullOrWhiteSpace(path)) return;
            var ontology = await _fileService.LoadDTOAsync<Ontology>(path, DataType.Ontology);
            if (ontology is null) return;
            ontology.Entities ??= [];
            Ontologys.Add(ontology);
            await _fileService.SaveDTOAsync(Ontologys, DataType.Ontology, GlobalConfig.OntologyPath);
        }); // обработчик нажатия на кнопку "Загрузить" на OntologyView

        public RelayCommand AddCommand => GetCommand(o =>
        {
            IsEnabledCheckBox = SelectedItem is Ontology;
            AddNewEntity = false;
            IsAdd = true;
            _manager = new(this);
            _manager.ShowDialog();
        }); // обработчик нажатия на кнопку "Добавить" на OntologyView

        public RelayCommand ChangeCommand => GetCommand(o =>
        {
            if (SelectedItem is null) return;
            IsEnabledCheckBox = false;
            AddNewEntity = false;
            IsAdd = false;
            EnteredName = SelectedItem.Name;
            EnteredDescription = SelectedItem.Description;
            _manager = new(this);
            _manager.ShowDialog();
        }, o => Ontologys.Count != 0 && SelectedItem is not null); // обработчик нажатия на кнопку "Изменить" на OntologyView

        public RelayCommand DeleteCommand => GetCommand(async o =>
        {
            if (SelectedItem is Ontology ontology)
            {
                Ontologys.Remove(ontology);
            }
            else if (SelectedItem is Entitie entitie)
            {
                var parent = Ontologys.FirstOrDefault(o => o.Entities?.Contains(entitie) == true);
                parent?.Entities.Remove(entitie);
            }
            await _fileService.SaveDTOAsync(Ontologys, DataType.Ontology, GlobalConfig.OntologyPath);
            SelectedItem = null;
        }, o => Ontologys.Count != 0 && SelectedItem is not null); // обработчик нажатия на кнопку "Удалить" на OntologyView

        public RelayCommand ApplyCommand => GetCommand(async o =>
        {
            var enteredName = EnteredName?.Trim();
            var enteredDescription = EnteredDescription?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(enteredName))
            {
                System.Windows.MessageBox.Show("Укажите название.");
                return;
            }

            if (IsAdd)
            {
                if (AddNewEntity)
                {
                    if (SelectedItem is not Ontology ontology)
                    {
                        System.Windows.MessageBox.Show("Чтобы добавить сущность, выберите онтологию.");
                        return;
                    }

                    ontology.Entities ??= [];
                    ontology.Entities.Add(new()
                    {
                        Name = enteredName,
                        Description = enteredDescription
                    });
                }
                else
                {
                    Ontologys.Add(new()
                    {
                        Name = enteredName,
                        Description = enteredDescription,
                        Entities = []
                    });
                }
            }
            else
            {
                if (SelectedItem is null)
                {
                    System.Windows.MessageBox.Show("Выберите элемент для изменения.");
                    return;
                }

                SelectedItem.Name = enteredName;
                SelectedItem.Description = enteredDescription;
            }
            ResetUIManager();
            _manager?.Close();
            await _fileService.SaveDTOAsync(Ontologys, DataType.Ontology, GlobalConfig.OntologyPath);
            SelectedItem = null;
        }); // обработчик нажатия на кнопку "Применить" на OntologyManagerView

        public RelayCommand BackCommand => GetCommand(o =>
        {
            ResetUIManager();
            _manager?.Close();
            SelectedItem = null;
        }); // обработчик нажатия на кнопку "Назад" на OntologyManagerView

        public RelayCommand SelectedItemChangedCommand => GetCommand(selectedItem =>
        {
            SelectedItem = selectedItem as IObjectOntology;
        }); // обработчик выбора элемента в TreeView на OntologyView

        private void ResetUIManager()
        {
            EnteredName = string.Empty;
            EnteredDescription = string.Empty;
            AddNewEntity = false;
            IsEnabledCheckBox = false;
        } // сброс полей на OntologyManagerView
    }
}

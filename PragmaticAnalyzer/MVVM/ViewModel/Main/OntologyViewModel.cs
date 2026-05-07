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
            IsEnabledCheckBox = true;
        }

        public RelayCommand LoadCommand => GetCommand(async o =>
        {
            var path = DialogService.OpenFileDialog(DialogService.JsonFilter);
            if (path is null) return;
            var ontology = await _fileService.LoadDTOAsync<Ontology>(path, DataType.Ontology);
            if (ontology is null) return;
            Ontologys.Add(ontology);
            await _fileService.SaveDTOAsync(Ontologys, DataType.Ontology, GlobalConfig.OntologyPath);
        }); // обработчик нажатия на кнопку "Загрузить" на OntologyView

        public RelayCommand AddCommand => GetCommand(o =>
        {
            IsEnabledCheckBox = true;
            IsAdd = true;
            _manager = new(this);
            _manager.ShowDialog();
        }); // обработчик нажатия на кнопку "Добавить" на OntologyView

        public RelayCommand ChangeCommand => GetCommand(o =>
        {
            IsEnabledCheckBox = false;
            IsAdd = false;
            if (SelectedItem is Ontology)
            {
                EnteredName = SelectedItem.Name;
                EnteredDescription = SelectedItem.Description;
            }
            else if (SelectedItem is Entitie)
            {
                EnteredName = SelectedItem.Name;
                EnteredDescription = SelectedItem.Description;
            }
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
                var parent = Ontologys.FirstOrDefault(o => o.Entities.Contains(entitie));
                parent?.Entities.Remove(entitie);
            }
            await _fileService.SaveDTOAsync(Ontologys, DataType.Ontology, GlobalConfig.OntologyPath);
            SelectedItem = null;
        }, o => Ontologys.Count != 0 && SelectedItem is not null); // обработчик нажатия на кнопку "Удалить" на OntologyView

        public RelayCommand ApplyCommand => GetCommand(async o =>
        {
            if (IsAdd)
            {
                if (SelectedItem is Ontology ontology && AddNewEntity)
                {
                    ontology.Entities ??= [];
                    ontology.Entities.Add(new()
                    {
                        Name = EnteredName,
                        Description = EnteredDescription
                    });
                }
                else
                {
                    Ontologys.Add(new()
                    {
                        Name = EnteredName,
                        Description = EnteredDescription
                    });
                }
            }
            else
            {
                SelectedItem.Name = EnteredName;
                SelectedItem.Description = EnteredDescription;
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
            SelectedItem = (IObjectOntology)selectedItem;
        }); // обработчик выбора элемента в TreeView на OntologyView

        private void ResetUIManager()
        {
            EnteredName = string.Empty;
            EnteredDescription = string.Empty;
            AddNewEntity = false;
        } // сброс полей на OntologyManagerView
    }
}
using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.Core;
using PragmaticAnalyzer.Databases;
using PragmaticAnalyzer.Enums;
using PragmaticAnalyzer.MVVM.Views.Main;
using PragmaticAnalyzer.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace PragmaticAnalyzer.MVVM.ViewModel.Main
{
    public class CreatorViewModel : ViewModelBase
    {
        private readonly IFileService _fileService;
        private readonly Func<object, string, DataType, Task> SaveDatabase;
        private readonly Action<string> DeleteDatabase;
        private DatabaseManagerView? _databaseManager;
        private RecordManagerView? _recordManager;
        private bool _isAddDatabase;
        private bool _isAddRecord;

        public ObservableCollection<DynamicDatabase> Databases { get; set; }
        public DynamicDatabase? SelectedDatabase { get => Get<DynamicDatabase>(); set => Set(value); }
        public string EnteredNameDatabase { get => Get<string>(); set => Set(value); }
        public string? EnteredNameField { get => Get<string>(); set => Set(value); }
        public string? EnteredNameIndexPrefix { get => Get<string>(); set => Set(value); }
        public ObservableCollection<string>? EnteredFieldsDatabase { get; set; }
        public string SelectedFieldDatabase { get => Get<string>(); set => Set(value); }
        public bool IsEnabledEnteredNameDatabase { get => Get<bool>(); set => Set(value); }

        public ObservableCollection<FieldInput>? FieldInputs { get => Get<ObservableCollection<FieldInput>>(); set => Set(value); }
        public DynamicRecord? SelectedRecord { get => Get<DynamicRecord>(); set => Set(value); }
        public string? EnteredDescriptionField { get => Get<string>(); set => Set(value); }

        public CreatorViewModel(ObservableCollection<DynamicDatabase> databases, Func<object, string, DataType, Task> saveDatabase, Action<string> deleteDatabase)
        {
            Databases = databases;
            SaveDatabase = saveDatabase;
            DeleteDatabase = deleteDatabase;
            _fileService = new FileService();
            IsEnabledEnteredNameDatabase = true;
        }

        public RelayCommand AddDatabaseCommand => GetCommand(o =>
        {
            EnteredNameDatabase = "unnamedDb";
            _isAddDatabase = true;
            EnteredFieldsDatabase = [];
            _databaseManager = new(this);
            _databaseManager.ShowDialog();
        });

        public RelayCommand ChangeDatabaseCommand => GetCommand(o =>
        {
            _isAddDatabase = false;
            IsEnabledEnteredNameDatabase = false;
            EnteredNameDatabase = SelectedDatabase.Name;
            EnteredFieldsDatabase = new(SelectedDatabase.CustomFieldNames);
            EnteredNameIndexPrefix = SelectedDatabase.IndexPrefix;
            _databaseManager = new(this);
            _databaseManager.ShowDialog();
        }, o => SelectedDatabase is not null);

        public RelayCommand DeleteDatabaseCommand => GetCommand(async o =>
        {
            DeleteDatabase?.Invoke(SelectedDatabase.Name);
            Databases.Remove(SelectedDatabase);
            await _fileService.SaveDTOAsync(Databases, DataType.SchemeDatabase, GlobalConfig.SchemeDatabasePath);
        }, o => SelectedDatabase is not null);

        public RelayCommand AddFieldInDatabaseCommand => GetCommand(o =>
        {
            if (string.Equals(EnteredNameField, "Описание", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Запрещено использовать ключ = Описание (описание)");
                return;
            }
            foreach (var enteredField in EnteredFieldsDatabase)
            {
                if (enteredField == EnteredNameField)
                {
                    MessageBox.Show("Запрещено использовать повторяющийся ключ");
                    return;
                }
            }
            EnteredFieldsDatabase?.Add(EnteredNameField ?? string.Empty);
        }, o => EnteredNameField != string.Empty && EnteredNameField != null);

        public RelayCommand RemoveFieldInDatabaseCommand => GetCommand(o =>
        {
            EnteredFieldsDatabase?.Remove(SelectedFieldDatabase);
        }, o => SelectedFieldDatabase != null);

        public RelayCommand ApplyDatabaseManagerCommand => GetCommand(async o =>
        {
            if (string.IsNullOrEmpty(EnteredNameDatabase))
            {
                MessageBox.Show("Поле с названием базы данных должно быть заполнено");
                return;
            }

            if (_isAddDatabase)
            {
                if (Databases.Any(item => string.Equals(item.Name, EnteredNameDatabase, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("База данных с таким именем уже существует");
                    return;
                }
                Databases.Add(new(EnteredNameDatabase.Replace(" ", ""), EnteredNameIndexPrefix, EnteredFieldsDatabase ?? []));
            }
            else
            {
                if (SelectedDatabase.Name != EnteredNameDatabase)
                {
                    DeleteDatabase?.Invoke(SelectedDatabase.Name);
                }
                SelectedDatabase.ChangeScheme(EnteredNameDatabase, EnteredNameIndexPrefix, EnteredFieldsDatabase ?? []);
                SaveDatabase?.Invoke(SelectedDatabase.Records, EnteredNameDatabase, DataType.DunamicDatabase);
            }
            await _fileService.SaveDTOAsync(Databases, DataType.SchemeDatabase, GlobalConfig.SchemeDatabasePath);
            CompleteDatabaseManager();
        });

        public RelayCommand CloseDatabaseManagerCommand => GetCommand(o =>
        {
            CompleteDatabaseManager();
        });

        public RelayCommand AddRecordCommand => GetCommand(o =>
        {
            FieldInputs = new ObservableCollection<FieldInput>(SelectedDatabase.CustomFieldNames.Select(name => new FieldInput(name)));
            _isAddRecord = true;
            _recordManager = new(this);
            _recordManager?.ShowDialog();
        }, o => SelectedDatabase is not null);

        public RelayCommand ChangeRecordCommand => GetCommand(o =>
        {
            FieldInputs = [];
            EnteredDescriptionField = SelectedRecord.Description;
            foreach (var fieldName in SelectedDatabase.CustomFieldNames)
            {
                var fieldInput = new FieldInput(fieldName)
                {
                    Value = SelectedRecord[fieldName]
                };
                FieldInputs.Add(fieldInput);
            }
            _isAddRecord = false;
            _recordManager = new(this);
            _recordManager.ShowDialog();
        }, o => SelectedDatabase is not null && SelectedRecord is not null);

        public RelayCommand DeleteRecordCommand => GetCommand(o =>
        {
            if (SelectedDatabase.Records.Count == 1)
            {
                DeleteDatabase?.Invoke(SelectedDatabase.Name);
                SelectedDatabase.Records.Remove(SelectedRecord);
            }
            else
            {
                SelectedDatabase.Records.Remove(SelectedRecord);
                SaveDatabase?.Invoke(SelectedDatabase.Records, SelectedDatabase.Name, DataType.DunamicDatabase);
            }

        }, o => SelectedDatabase is not null && SelectedRecord is not null);

        public RelayCommand ApplyRecordManagerCommand => GetCommand(o =>
        {
            var customFields = FieldInputs.ToDictionary(
                 input => input.Name,
                 input => input.Value ?? string.Empty
              );
            if (_isAddRecord)
            {
                SelectedDatabase.AddRecord(SelectedDatabase.Name, EnteredDescriptionField, customFields);
            }
            else
            {
                DynamicRecord record = new(SelectedRecord.GuidId, SelectedRecord.IndexValue)
                {
                    NameDatadase = SelectedDatabase.Name,
                    Description = EnteredDescriptionField,
                    Fields = customFields
                };
                SelectedDatabase.ChangeRecord(record);
            }
            SaveDatabase?.Invoke(SelectedDatabase.Records, SelectedDatabase.Name, DataType.DunamicDatabase);
            CompleteRecordManager();
            FieldInputs = null;
        });

        public RelayCommand CloseRecordManagerCommand => GetCommand(o =>
        {
            CompleteRecordManager();
        });

        public void CompleteDatabaseManager()
        {
            EnteredNameDatabase = null;
            EnteredNameField = null;
            EnteredFieldsDatabase = null;
            EnteredNameIndexPrefix = null;
            SelectedDatabase = null;
            IsEnabledEnteredNameDatabase = true;
            _databaseManager?.Close();
        }

        public void CompleteRecordManager()
        {
            EnteredDescriptionField = null;
            SelectedRecord = null;
            FieldInputs = null;
            _recordManager?.Close();
        }
    }

    public class FieldInput(string name) : ViewModelBase
    {
        public string Name { get; } = name;
        public string Value { get => Get<string>(); set => Set(value); }
    }
}
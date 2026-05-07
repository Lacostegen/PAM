using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json.Serialization;

namespace PragmaticAnalyzer.Databases
{
    public class DynamicDatabase : ViewModelBase
    {
        private int RecordCounter { get; set; } // счетчик записей
        public Guid GuidId { get; set; } = Guid.NewGuid(); // уникальный идентификатор
        public string Name { get => Get<string>(); private set => Set(value); } // наименование базы данных
        public string? IndexPrefix
        {
            get => Get<string>();
            set
            {
                Set(value ?? "Запись");
            }
        }  // префикс к индексу
        public ObservableCollection<string> CustomFieldNames { get; private set; } // список наименование кастомных полей
        [JsonIgnore]
        public ObservableCollection<DynamicRecord> Records { get; private set; } // список записей

        public DynamicDatabase(string name, string? indexPrefix, ObservableCollection<string> customFieldNames)
        {
            Name = name;
            IndexPrefix = indexPrefix;
            CustomFieldNames = customFieldNames ?? [];
            Records = [];
            Records.CollectionChanged += OnRecordsCollectionChanged;
        }

        private void OnRecordsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                int startIndex = e.OldStartingIndex;

                for (int i = startIndex; i < Records.Count; i++)
                {
                    var newIndex = i;
                    Records[i].IndexValue = $"{IndexPrefix}-{newIndex}";
                }
                RecordCounter--;
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                RecordCounter++;
            }
        } // изменение индекса записи

        private void SyncRecordsWithSchema()
        {
            var validFieldNames = new HashSet<string>(CustomFieldNames, StringComparer.OrdinalIgnoreCase);

            foreach (var record in Records)
            {
                var keysToRemove = record.Fields.Keys.ToList()
                                         .Where(key => !validFieldNames.Contains(key))
                                         .ToList();

                foreach (var key in keysToRemove)
                {
                    record.Fields.Remove(key);
                }
            }
        } // синхронизация базы данных со схемой, при ее изменении

        public void ChangeScheme(string? name, string? indexPrefix, ObservableCollection<string>? customFieldName)
        {
            if (name != null && name != string.Empty && Name != name)
            {
                Name = name;
            }
            if (indexPrefix != null && indexPrefix != string.Empty && IndexPrefix != indexPrefix)
            {
                IndexPrefix = indexPrefix;
                RecordCounter = 1;
                foreach (var record in Records)
                {
                    record.IndexValue = IndexPrefix + "-" + RecordCounter;
                    RecordCounter++;
                }
            }
            if (customFieldName != null)
            {
                CustomFieldNames.Clear();
                foreach (var customField in customFieldName)
                {
                    CustomFieldNames.Add(customField);
                }

                SyncRecordsWithSchema();
            }
        } // изменение схемы

        public void AddRecord(string nameDatabase, string? description, Dictionary<string, string> customFields)
        {
            var record = new DynamicRecord(IndexPrefix + "-" + RecordCounter)
            {
                NameDatadase = nameDatabase,
                Description = description,
            };
            foreach (var field in CustomFieldNames)
            {
                record[field] = customFields.TryGetValue(field, out var value) ? value : string.Empty;
            }
            Records.Add(record);
        } // добавление записи в базу данных

        public bool ChangeRecord(DynamicRecord record)
        {
            for (int i = 0; i < Records.Count; i++)
            {
                if (Records[i].GuidId == record.GuidId)
                {
                    Records[i].Description = record.Description;
                    Records[i].Fields = record.Fields;
                    return true;
                }
            }
            return false;
        } // изменение существующей записи
    } // схема базы данных, хранящая записи базы данных. Реализация методов управления схемой и базой данных

    public class DynamicRecord : ViewModelBase, IHasId, IHasDescription
    {
        [JsonIgnore]
        public string NameDatadase { get => Get<string>(); set => Set(value); } // наименование базы данных к которой принадлежит запись
        public Guid GuidId { get; set; } = Guid.NewGuid(); // уникальный идентификатор
        public string IndexValue { get => Get<string>(); internal set => Set(value); } // порядковый номер (значение индекса)
        public string? Description
        {
            get => Get<string>();
            set
            {
                Set(value ?? "Отсутствует");
            }
        } // обязательное поле - описание
        public Dictionary<string, string> Fields { get => Get<Dictionary<string, string>>(); set => Set(value); } // словарь кастомных полей с их значением

        public DynamicRecord(string indexValue)
        {
            IndexValue = indexValue;
            Description = string.Empty;
            Fields = [];
        }

        internal DynamicRecord(Guid guidId, string indexValue)
        {
            GuidId = guidId;
            IndexValue = indexValue;
        }

        public string this[string fieldName]
        {
            get => Fields.TryGetValue(fieldName, out var value) ? value : string.Empty;
            set => Fields[fieldName] = value;
        } // индексатор
    } // представление динамической записи
}
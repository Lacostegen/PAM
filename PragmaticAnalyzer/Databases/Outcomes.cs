using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PragmaticAnalyzer.Databases
{
    public class Outcomes
    {
        public ObservableCollection<Technology> Technologys { get; set; } = []; // база данных технологий оценки
        public ObservableCollection<Consequence> Consequences { get; set; } = []; // база данных негативных последствий (рисков)
    } // представление исхода (риска)

    public class Technology : ViewModelBase, IHasId
    {
        public Guid GuidId { get; set; } = Guid.NewGuid();

        [Description("Порядковый номер")]
        public string SequenceNumber { get => Get<string>(); set => Set(value); }

        [Description("Метод")]
        public string MethodName { get => Get<string>(); set => Set(value); }

        [Description("Описание")]
        public string Description { get => Get<string>(); set => Set(value); }

        [Description("Применение")]
        public string Usage { get => Get<string>(); set => Set(value); }

        [Description("Масштаб")]
        public string Scale { get => Get<string>(); set => Set(value); }

        [Description("Временной горизонт")]
        public string Horizont { get => Get<string>(); set => Set(value); }

        [Description("Уровень принятия решения")]
        public string Level { get => Get<string>(); set => Set(value); }

        [Description("Необходимость информации/данных")]
        public string Necessity { get => Get<string>(); set => Set(value); }

        [Description("Опыт специалиста")]
        public string Experience { get => Get<string>(); set => Set(value); }

        [Description("Качеств/колличеств")]
        public string Сharacteristic { get => Get<string>(); set => Set(value); }

        [Description("Применение усилий")]
        public string Effort { get => Get<string>(); set => Set(value); }
    } // представление технологии оценки

    public class Consequence : ViewModelBase, IHasId
    {
        public Guid GuidId { get; } = Guid.NewGuid();

        [Description("Порядковый номер")]
        public string Number { get => Get<string>(); set => Set(value); }

        [Description("Наименование последствия")]
        public string Name { get => Get<string>(); set => Set(value); }

        [Description("Ущерб последствия")]
        public string Damage { get => Get<string>(); set => Set(value); }

        public ObservableCollection<string> NameThreats { get; set; }

        [JsonIgnore]
        [Description("Связанные угрозы")]
        public string NameThreatsToString => string.Join(", ", NameThreats);
    } // представление негативного последствия (риска)
}
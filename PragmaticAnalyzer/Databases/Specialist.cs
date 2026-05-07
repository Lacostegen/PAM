using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PragmaticAnalyzer.Databases
{
    public class Specialist : ViewModelBase, IHasId
    {
        public Guid GuidId { get; set; } = Guid.NewGuid();

        [Description("Наименование ОВУ")]
        public string NameOrgan { get => Get<string>(); set => Set(value); }

        [Description("Наименование вышестоящего ОВУ ")]
        public string NameHighestOrgan { get => Get<string>(); set => Set(value); }

        [Description("Наименование подчиненного ОВУ ")]
        public string NameSubordinateOrgan { get => Get<string>(); set => Set(value); }

        [Description("Статус по выявленной уязвимости")]
        public string StatusVulnerability { get => Get<string>(); set => Set(value); }

        [Description("Предпринятые действия")]
        public string ActionsTaken { get => Get<string>(); set => Set(value); }

        [Description("Наименование СС СОПКА")]
        public string NameSoftware { get => Get<string>(); set => Set(value); }

        [Description("Наименование взаимодействующих ОВУ ")]
        public ObservableCollection<string> NameInteractingOrgans { get; set; } = [];

        [Description("Список принятых мер защиты ")]
        public ObservableCollection<string> UsingMeasures { get; set; } = [];

        [JsonIgnore]
        public string NameInteractingOrgansToString => string.Join(", ", NameInteractingOrgans);

        [JsonIgnore]
        public string UsingMeasuresToString => string.Join(", ", UsingMeasures);
    } // представление специалиста по ЗИ
}
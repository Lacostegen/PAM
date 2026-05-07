using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PragmaticAnalyzer.Databases
{
    public class Violator : ViewModelBase, IHasId, IHasDescription
    {
        public Guid GuidId { get; set; } = Guid.NewGuid(); // уникальный идентификатор

        [Description("Описание")]
        public string Description { get => Get<string>(); set => Set(value); }

        [Description("Название группировки")]
        public string GroupName { get => Get<string>(); set => Set(value); }

        [Description("Государственная принадлежность")]
        public string StateAffiliation { get => Get<string>(); set => Set(value); }

        [Description("Второстепенные названия")]
        public string AlternateNames { get => Get<string>(); set => Set(value); }

        [Description("Объекты атаки")]
        public string AttackTargets { get => Get<string>(); set => Set(value); }

        [Description("Известные атаки")]
        public string KnownAttacks { get => Get<string>(); set => Set(value); }

        [Description("Применяемые инструменты")]
        public string UsedTools { get => Get<string>(); set => Set(value); }

        [Description("Цели атаки")]
        public string AttackObjectives { get => Get<string>(); set => Set(value); }

        [Description("Применяемые тактики")]
        public ObservableCollection<string> TacticsUsed { get; set; } = [];

        [JsonIgnore]
        [Description("Применяемые тактики")]
        public string TacticsUsedToString => string.Join(",\n\n", TacticsUsed);
    } // представление нарушителя
}
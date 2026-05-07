using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PragmaticAnalyzer.Databases
{
    public class ProtectionMeasure : ViewModelBase, IHasId
    {
        public Guid GuidId { get; set; } = Guid.NewGuid();

        [Description("Наименование группы мер защиты")]
        public string NameGroup { get => Get<string>(); set => Set(value); }

        public string Name { get => Get<string>(); set => Set(value); } //ShortName

        public string Number { get => Get<string>(); set => Set(value); }

        [Description("Наименование меры защиты")]
        public string FullName { get => Get<string>(); set => Set(value); }

        [Description("Классы защищенности автоматизированной системы управления")]
        public string SecurityClasses { get => Get<string>(); set => Set(value); }

        [JsonIgnore]
        [Description("Условное обозначение")]
        public string DispayedName => Name + "." + Number;
    } // представление меры защиты
}
using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;
using System.ComponentModel;

namespace PragmaticAnalyzer.Databases
{
    public class Threat : ViewModelBase, IHasId, IHasDescription
    {
        public Guid GuidId { get; set; } = Guid.NewGuid(); // уникальный идентификатор

        [Description("Идентификатор УБИ")]
        public string Id { get => Get<string>(); set => Set(value); }

        [Description("Наименование УБИ")]
        public string Name { get => Get<string>(); set => Set(value); }

        [Description("Описание")]
        public string Description { get => Get<string>(); set => Set(value); }

        [Description("Источник угрозы")]
        public string Source { get => Get<string>(); set => Set(value); }

        [Description("Объект воздействия")]
        public string ObjectInfluence { get => Get<string>(); set => Set(value); }

        [Description("Нарушение конфиденциальности")]
        public string PrivacyViolation { get => Get<string>(); set => Set(value); }

        [Description("Нарушение целостности")]
        public string IntegrityViolation { get => Get<string>(); set => Set(value); }

        [Description("Нарушение доступности")]
        public string AccessibilityViolation { get => Get<string>(); set => Set(value); }

        [Description("Дата включвения угрозы в БнД УБИ")]
        public string DateInclusion { get => Get<string>(); set => Set(value); }

        [Description("Дата последнего изменения")]
        public string DateChange { get => Get<string>(); set => Set(value); }
    } // представление угрозы
}
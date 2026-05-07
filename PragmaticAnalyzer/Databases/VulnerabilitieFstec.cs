using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;
using System.ComponentModel;

namespace PragmaticAnalyzer.Databases
{
    public class VulnerabilitieFstec : ViewModelBase, IHasId, IHasDescription
    {
        public Guid GuidId { get; set; } = Guid.NewGuid(); // уникальный идентификатор

        [Description("Идентификатор")]
        public string Identifier { get => Get<string>(); set => Set(value); }

        [Description("Наименование уязвимости")]
        public string Name { get => Get<string>(); set => Set(value); }

        [Description("Описание уязвимости")]
        public string Description { get => Get<string>(); set => Set(value); }

        [Description("Вендор ПО")]
        public string Vendor { get => Get<string>(); set => Set(value); } 

        [Description("Название ПО")]
        public string NameSoftware { get => Get<string>(); set => Set(value); } 

        [Description("Версия ПО")]
        public string Version { get => Get<string>(); set => Set(value); } 

        [Description("Тип ПО")]
        public string Type { get => Get<string>(); set => Set(value); }

        [Description("Наименование ОС и тип аппаратной платформы")]
        public string NameOperatingSystem { get => Get<string>(); set => Set(value); }

        [Description("Класс уязвимости")]
        public string Class { get => Get<string>(); set => Set(value); }

        [Description("Дата выявления")]
        public string Date { get => Get<string>(); set => Set(value); }

        [Description("CVSS 2.0")]
        public string CvssTwo { get => Get<string>(); set => Set(value); }

        [Description("CVSS 3.0")]
        public string CvssThree { get => Get<string>(); set => Set(value); }

        [Description("Уровень опасности уязвимости")]
        public string DangerLevel { get => Get<string>(); set => Set(value); }

        [Description("Возможные меры по устранению")]
        public string Measure { get => Get<string>(); set => Set(value); }

        [Description("Наличие эксплойта")]
        public string Exploit { get => Get<string>(); set => Set(value); }

        [Description("Информация об устранении")]
        public string Information { get => Get<string>(); set => Set(value); }

        [Description("Ссылки на источники")]
        public string Links { get => Get<string>(); set => Set(value); }

        [Description("Идентификаторы других систем описаний уязвимости")]
        public string OtherIdentifier { get => Get<string>(); set => Set(value); }

        [Description("Прочая информация")]
        public string OtherInformation { get => Get<string>(); set => Set(value); }

        [Description("Связь с инцидентами ИБ")]
        public string Incident { get => Get<string>(); set => Set(value); }

        [Description("Способ эксплуатации")]
        public string OperatingMethod { get => Get<string>(); set => Set(value); }

        [Description("Способ устранения")]
        public string EliminationMethod { get => Get<string>(); set => Set(value); }

        [Description("Описание ошибки CWE")]
        public string DescriptionCwe { get => Get<string>(); set => Set(value); }

        [Description("Тип ошибки CWE")]
        public string Cwe { get => Get<string>(); set => Set(value); }
    } // представление уязвимости ФСТЭК
}
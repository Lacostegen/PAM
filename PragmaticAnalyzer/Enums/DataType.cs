using System.ComponentModel;

namespace PragmaticAnalyzer.Enums
{
    public enum  DataType
    {
        LastUpdateConfig = 1, // конфигурационный файл с датами последнего обновления баз данных

        VulConfig = 2, // конфигурационный файл с настройками парсинга уязвимостей

        ThreatConfig = 3, // конфигурационный файл с настройками парсинга угроз

        ExploitConfig = 4, // конфигурационный файл с с нстройками парсинга экслойтов

        WordTwoVecConfig = 16, // конфигурация для Word2Vec

        FastTextConfig = 17, // конфигурация для FastText

        TfIdfConfig = 18,  // конфигурация для Tf-Idf

        AvailableDatabasesConfig = 19, // используемые базы данных

        SchemeDatabase = 20, // схема базы данных

        [Description("Угрозы")]
        Threat = 5,

        [Description("ФСТЭК")]
        VulnerabilitiesFstec = 6,

        [Description("Нарушитель")]
        Violator = 7,

        [Description("Техники и тактики")]
        Tactic = 8,

        [Description("Текущее состояние")]
        CurrentStatus = 9,

        [Description("Эталонное состояние")]
        ReferenceStatus = 10,

        [Description("Специалист по ЗИ")]
        Specialist = 11,

        [Description("Меры защиты")]
        ProtectionMeasures = 12,

        [Description("Экслойты")]
        Exploit = 13,

        [Description("Риски")]
        Outcomes = 14,

        [Description("Онтологии")]
        Ontology = 15, 
 
        [Description("Динамическая")]
        DunamicDatabase = 21, 

        [Description("NVD")]
        VulnerabilitiesNvd = 22,

        [Description("JVN")]
        VulnerabilitiesJvn = 23,

        [Description("Расширенная")]
        VulnerabilitiesExtended = 24,

        [Description("Русифицированная NVD")]
        VulnerabilitiesNvdTranslated = 25,

        [Description("Русифицированная JVN")]
        VulnerabilitiesJvnTranslated = 26,
    } // перечисление имеющихся типов DTO в приложении
}
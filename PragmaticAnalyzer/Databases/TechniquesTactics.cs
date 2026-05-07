using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PragmaticAnalyzer.Databases
{
    public interface IEntityTit
    {
        string Name { get; set; }
        string Description { get; set; }
    } // интерфейс для техники и тактики

    public class Tactic : ViewModelBase, IEntityTit, IHasId, IHasDescription
    {
        public Guid GuidId { get; set; } = Guid.NewGuid(); // уникальный идентификатор

        public string Name { get => Get<string>(); set => Set(value); } 

        [Description("Описание")]
        public string Description { get => Get<string>(); set => Set(value); }

        public ObservableCollection<Technique> Techniques { get; set; }

        [JsonIgnore]
        [Description("Наименование")]
        public string DisplayName => $"Тактика: {Name}";
    } // предсавление тактики

    public class Technique : ViewModelBase, IEntityTit, IHasId
    {
        public Guid GuidId { get; set; } = Guid.NewGuid(); // уникальный идентификатор

        public string Name { get => Get<string>(); set => Set(value); }

        [Description("Описание")]
        public string Description { get => Get<string>(); set => Set(value); }

        [JsonIgnore]
        [Description("Наименование")]
        public string DisplayName => $"Техника: {Name}";
    } // представление техники
}
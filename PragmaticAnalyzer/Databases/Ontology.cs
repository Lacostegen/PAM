using PragmaticAnalyzer.Abstractions;
using PragmaticAnalyzer.Core;
using System.Collections.ObjectModel;

namespace PragmaticAnalyzer.Databases
{
    public interface IObjectOntology
    {
        string Name { get; set; }
        string Description { get; set; }
    } // интерфейс для онтологии и сущности

    public class Ontology : ViewModelBase, IHasId, IObjectOntology
    {
        public Guid GuidId { get; set; } = Guid.NewGuid(); // уникальный идентификатор
        public string Name { get => Get<string>(); set => Set(value); } // наименование
        public string Description { get => Get<string>(); set => Set(value); } // опсиание
        public ObservableCollection<Entitie> Entities { get; set; } // список сущностей
    } // представление онтологии

    public class Entitie : ViewModelBase, IHasId, IObjectOntology
    {
        public Guid GuidId { get; set; } = Guid.NewGuid(); // уникальный идентификатор
        public string Name { get => Get<string>(); set => Set(value); } // наименование
        public string Description { get => Get<string>(); set => Set(value); } // опсиание
    } // представление сущности
}
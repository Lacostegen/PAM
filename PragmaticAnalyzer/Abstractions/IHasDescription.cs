namespace PragmaticAnalyzer.Abstractions
{
    /// <summary>
    /// Интерфейс для баз данных имеющих описание
    /// </summary>
    public interface IHasDescription
    {
        string Description { get; set; }
    }
}
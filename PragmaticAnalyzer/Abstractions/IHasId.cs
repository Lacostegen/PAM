namespace PragmaticAnalyzer.Abstractions
{
    /// <summary>
    /// Интерфейс для баз данных имеющих Guid
    /// </summary>
    public interface IHasId
    {
        Guid GuidId { get; }
    }
}
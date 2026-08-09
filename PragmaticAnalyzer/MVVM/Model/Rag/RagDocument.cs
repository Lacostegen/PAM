using System.Collections.Generic;

namespace PragmaticAnalyzer.MVVM.Model.Rag
{
    public class RagDocument
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Например: database, manual.
        /// </summary>
        public string SourceKind { get; set; } = string.Empty;

        /// <summary>
        /// Например: Threat, VulnerabilitieFstec, manual_kaspersky.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Например: threat, vulnerability, violator, manual.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Например: Kaspersky, Secret Net Studio, Dr.Web.
        /// </summary>
        public string Product { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Раздел руководства или название сущности базы.
        /// </summary>
        public string Section { get; set; } = string.Empty;

        /// <summary>
        /// Номер страницы, если документ получен из PDF.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Номер фрагмента внутри документа.
        /// </summary>
        public int ChunkIndex { get; set; }

        /// <summary>
        /// Текст для поиска. Может содержать ключевые слова, синонимы, ID.
        /// </summary>
        public string SearchText { get; set; } = string.Empty;

        /// <summary>
        /// Короткий текст, который будет передаваться модели в prompt.
        /// </summary>
        public string PromptText { get; set; } = string.Empty;

        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
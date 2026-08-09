using PragmaticAnalyzer.Configs;
using PragmaticAnalyzer.MVVM.Model.Rag;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PragmaticAnalyzer.Services.Rag
{
    public class RagIndexService
    {
        private readonly RagSearchService _searchService;
        private readonly RagPromptBuilder _promptBuilder;
        private readonly RagDocumentFactory _documentFactory;

        private readonly List<RagDocument> _documents = new();
        private readonly RagIndexStorageService _storageService;
        private readonly ManualDocumentLoader _manualDocumentLoader;
        private readonly ProjectDatabaseRagLoader _projectDatabaseRagLoader;

        public int LoadManuals(string knowledgeBasePath)
        {
            var chunks = _manualDocumentLoader.LoadManualChunks(knowledgeBasePath);

            var documents = chunks.Select(chunk =>
                _documentFactory.CreateFromManualChunk(
                    chunk.Product,
                    chunk.Source,
                    chunk.Title,
                    chunk.Section,
                    chunk.Text,
                    chunk.Page,
                    chunk.ChunkIndex));

            var beforeCount = _documents.Count;

            AddDocuments(documents);

            return _documents.Count - beforeCount;
        }

        public int LoadProjectDatabases(string databasePath)
        {
            var documents = _projectDatabaseRagLoader.LoadProjectDatabaseDocuments(databasePath);

            var beforeCount = _documents.Count;

            AddDocuments(documents);

            return _documents.Count - beforeCount;
        }

        public async Task SaveDocumentsToDiskAsync(
            string knowledgeBasePath,
            IEnumerable<RagDocument> documents,
            RagIndexPart part,
            CancellationToken ct = default)
        {
            await _storageService.SaveDocumentsAsync(
                knowledgeBasePath,
                documents,
                part,
                ct);
        }

        public RagIndexService()
        {
            _searchService = new RagSearchService();
            _promptBuilder = new RagPromptBuilder();
            _documentFactory = new RagDocumentFactory();
            _manualDocumentLoader = new ManualDocumentLoader();
            _storageService = new RagIndexStorageService();
            _projectDatabaseRagLoader = new ProjectDatabaseRagLoader();
        }



        public async Task<int> LoadFromDiskAsync(
            string knowledgeBasePath,
            RagIndexPart part = RagIndexPart.All,
            CancellationToken ct = default)
        {
            var documents = await _storageService.LoadDocumentsAsync(
                knowledgeBasePath,
                part,
                ct);

            SetDocuments(documents);

            return DocumentCount;
        }

        public async Task SaveToDiskAsync(
            string knowledgeBasePath,
            RagIndexPart part = RagIndexPart.All,
            CancellationToken ct = default)
        {
            await _storageService.SaveDocumentsAsync(
                knowledgeBasePath,
                _documents,
                part,
                ct);
        }

        public bool HasSavedIndex(
            string knowledgeBasePath,
            RagIndexPart part = RagIndexPart.All)
        {
            return _storageService.IndexExists(knowledgeBasePath, part);
        }

        public IReadOnlyList<RagDocument> Documents => _documents;

        public int DocumentCount => _documents.Count;

        public bool HasDocuments => _documents.Count > 0;

        public void Clear()
        {
            _documents.Clear();
            _searchService.SetDocuments(_documents);
        }

        public void SetDocuments(IEnumerable<RagDocument> documents)
        {
            _documents.Clear();

            if (documents != null)
            {
                _documents.AddRange(documents.Where(document =>
                    document != null &&
                    !string.IsNullOrWhiteSpace(document.SearchText)));
            }

            _searchService.SetDocuments(_documents);
        }

        public void AddDocuments(IEnumerable<RagDocument> documents)
        {
            if (documents == null)
            {
                return;
            }

            _documents.AddRange(documents.Where(document =>
                document != null &&
                !string.IsNullOrWhiteSpace(document.SearchText)));

            _searchService.SetDocuments(_documents);
        }

        public void AddDatabaseItems<T>(
            IEnumerable<T> items,
            string source,
            string type)
        {
            var documents = _documentFactory.CreateFromDatabaseItems(
                items,
                source,
                type);

            AddDocuments(documents);
        }

        public void AddManualChunk(
            string product,
            string source,
            string title,
            string section,
            string text,
            int page,
            int chunkIndex)
        {
            var document = _documentFactory.CreateFromManualChunk(
                product,
                source,
                title,
                section,
                text,
                page,
                chunkIndex);

            AddDocuments(new[] { document });
        }

        public List<RagSearchResult> Search(
            string query,
            RagConfig config)
        {
            if (!HasDocuments)
            {
                return new List<RagSearchResult>();
            }

            return _searchService.Search(query, config);
        }

        public RagAnswerContext BuildAnswerContext(
            string query,
            RagConfig config)
        {
            config ??= new RagConfig();

            if (!config.IsEnabled || string.IsNullOrWhiteSpace(query))
            {
                return new RagAnswerContext
                {
                    Query = query,
                    ContextText = string.Empty,
                    Results = new List<RagSearchResult>()
                };
            }

            var results = Search(query, config);
            var contextText = _promptBuilder.BuildContext(results, config);

            return new RagAnswerContext
            {
                Query = query,
                ContextText = contextText,
                Results = results
            };
        }
    }
}
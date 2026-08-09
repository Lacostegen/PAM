using PragmaticAnalyzer.MVVM.Model.Rag;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PragmaticAnalyzer.Services.Rag
{
    public class RagIndexStorageService
    {
        private const string IndexFolderName = "Index";

        private const string AllIndexFileName = "rag_documents.json";
        private const string DatabasesIndexFileName = "rag_databases.json";
        private const string ManualsIndexFileName = "rag_manuals.json";
        private const string ManifestFileName = "rag_manifest.json";

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public string GetIndexFilePath(
            string knowledgeBasePath,
            RagIndexPart part = RagIndexPart.All)
        {
            var safeKnowledgeBasePath = string.IsNullOrWhiteSpace(knowledgeBasePath)
                ? Path.Combine(Environment.CurrentDirectory, "KnowledgeBase")
                : knowledgeBasePath;

            return Path.Combine(
                safeKnowledgeBasePath,
                IndexFolderName,
                GetIndexFileName(part));
        }

        public string GetManifestFilePath(string knowledgeBasePath)
        {
            var safeKnowledgeBasePath = string.IsNullOrWhiteSpace(knowledgeBasePath)
                ? Path.Combine(Environment.CurrentDirectory, "KnowledgeBase")
                : knowledgeBasePath;

            return Path.Combine(
                safeKnowledgeBasePath,
                IndexFolderName,
                ManifestFileName);
        }

        public bool IndexExists(
            string knowledgeBasePath,
            RagIndexPart part = RagIndexPart.All)
        {
            var indexPath = GetIndexFilePath(knowledgeBasePath, part);

            return File.Exists(indexPath);
        }

        public async Task<List<RagDocument>> LoadDocumentsAsync(
            string knowledgeBasePath,
            RagIndexPart part = RagIndexPart.All,
            CancellationToken ct = default)
        {
            var indexPath = GetIndexFilePath(knowledgeBasePath, part);

            if (!File.Exists(indexPath))
            {
                return new List<RagDocument>();
            }

            try
            {
                await using var stream = File.OpenRead(indexPath);

                var snapshot = await JsonSerializer.DeserializeAsync<RagIndexSnapshot>(
                    stream,
                    _jsonOptions,
                    ct);

                return snapshot?.Documents ?? new List<RagDocument>();
            }
            catch
            {
                return new List<RagDocument>();
            }
        }

        public async Task SaveDocumentsAsync(
            string knowledgeBasePath,
            IEnumerable<RagDocument> documents,
            RagIndexPart part = RagIndexPart.All,
            CancellationToken ct = default)
        {
            var indexPath = GetIndexFilePath(knowledgeBasePath, part);
            var indexDirectory = Path.GetDirectoryName(indexPath);

            if (!string.IsNullOrWhiteSpace(indexDirectory))
            {
                Directory.CreateDirectory(indexDirectory);
            }

            var documentList = documents == null
                ? new List<RagDocument>()
                : new List<RagDocument>(documents);

            var snapshot = new RagIndexSnapshot
            {
                CreatedAtUtc = DateTime.UtcNow,
                DocumentCount = documentList.Count,
                Documents = documentList
            };

            await using var stream = File.Create(indexPath);

            await JsonSerializer.SerializeAsync(
                stream,
                snapshot,
                _jsonOptions,
                ct);
        }

        public async Task<RagIndexManifest?> LoadManifestAsync(
            string knowledgeBasePath,
            CancellationToken ct = default)
        {
            var manifestPath = GetManifestFilePath(knowledgeBasePath);

            if (!File.Exists(manifestPath))
            {
                return null;
            }

            try
            {
                await using var stream = File.OpenRead(manifestPath);

                return await JsonSerializer.DeserializeAsync<RagIndexManifest>(
                    stream,
                    _jsonOptions,
                    ct);
            }
            catch
            {
                return null;
            }
        }

        public async Task SaveManifestAsync(
            string knowledgeBasePath,
            RagIndexManifest manifest,
            CancellationToken ct = default)
        {
            var manifestPath = GetManifestFilePath(knowledgeBasePath);
            var manifestDirectory = Path.GetDirectoryName(manifestPath);

            if (!string.IsNullOrWhiteSpace(manifestDirectory))
            {
                Directory.CreateDirectory(manifestDirectory);
            }

            manifest ??= new RagIndexManifest();

            await using var stream = File.Create(manifestPath);

            await JsonSerializer.SerializeAsync(
                stream,
                manifest,
                _jsonOptions,
                ct);
        }

        private static string GetIndexFileName(RagIndexPart part)
        {
            return part switch
            {
                RagIndexPart.Databases => DatabasesIndexFileName,
                RagIndexPart.Manuals => ManualsIndexFileName,
                _ => AllIndexFileName
            };
        }
    }
}
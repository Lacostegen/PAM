using System;
using System.IO;

namespace PragmaticAnalyzer.Configs
{
    public class RagConfig
    {
        public bool IsEnabled { get; set; } = true;

        public int TopK { get; set; } = 5;

        public int MaxCharsPerDocument { get; set; } = 1800;

        public int MaxTotalContextChars { get; set; } = 6000;

        public double MinScore { get; set; } = 1.0;

        public string KnowledgeBasePath { get; set; } = Path.Combine(
            Environment.CurrentDirectory,
            "KnowledgeBase");

        public string ProjectDatabasePath { get; set; } = Path.Combine(
        Environment.CurrentDirectory,
        "Database");

        public bool UseProjectDatabases { get; set; } = true;

        public bool UseManuals { get; set; } = true;

        public bool UseKasperskyManuals { get; set; } = true;

        public bool UseSecretNetStudioManuals { get; set; } = true;

        public bool UseDrWebManuals { get; set; } = true;

        public bool UseThreats { get; set; } = true;

        public bool UseVulnerabilities { get; set; } = true;

        public bool UseViolators { get; set; } = true;

        public bool UseProtectionMeasures { get; set; } = true;

        public bool UseTechniquesAndTactics { get; set; } = true;

        public bool UseExploits { get; set; } = true;

        public bool UseOutcomes { get; set; } = true;

        public bool NormalizePortablePaths()
        {
            var changed = false;
            var normalizedKnowledgeBasePath = ResolveDirectoryPath(KnowledgeBasePath, "KnowledgeBase");
            var normalizedProjectDatabasePath = ResolveDirectoryPath(ProjectDatabasePath, "Database");

            if (!string.Equals(KnowledgeBasePath, normalizedKnowledgeBasePath, StringComparison.OrdinalIgnoreCase))
            {
                KnowledgeBasePath = normalizedKnowledgeBasePath;
                changed = true;
            }

            if (!string.Equals(ProjectDatabasePath, normalizedProjectDatabasePath, StringComparison.OrdinalIgnoreCase))
            {
                ProjectDatabasePath = normalizedProjectDatabasePath;
                changed = true;
            }

            return changed;
        }

        private static string ResolveDirectoryPath(string configuredPath, string relativeDirectory)
        {
            if (!string.IsNullOrWhiteSpace(configuredPath) && Directory.Exists(configuredPath))
            {
                return configuredPath;
            }

            return Path.Combine(Environment.CurrentDirectory, relativeDirectory);
        }

    }
}

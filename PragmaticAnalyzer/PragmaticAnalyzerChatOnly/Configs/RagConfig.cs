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
    }
}
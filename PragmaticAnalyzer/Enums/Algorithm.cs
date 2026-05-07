using System.ComponentModel;

namespace PragmaticAnalyzer.Enums
{
    public enum Algorithm
    {
        [Description("FastText")]
        FastText = 0,

        [Description("Word2vec")]
        WordTwoVec = 1,

        [Description("Tf-idf")]
        TfIdf = 3      
    } // перечисление AI-алгоритмов в программе
}
using System.Collections.ObjectModel;

namespace PragmaticAnalyzer.WorkingServer.Matcher
{
    public class ResponseMatcher
    {   
        public ObservableCollection<MatcherObject> MatcherObjects { get; set; } // коллекция объектов, пришедших в ответе от сервера

        public class MatcherObject
        {
            public float Coefficient { get; set; } // коэффициент схожести с исходным текстом
            public Dictionary<Guid, string> Sources { get; set; } // словарь, где Guid - уникальный идентификатор записи, string - абсолютный путь к базе данных в которой находиться эта запись
        }
    } // ответ для запроса сопоставления источников
}
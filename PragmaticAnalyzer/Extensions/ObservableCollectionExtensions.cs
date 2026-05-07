using System.Collections.ObjectModel;

namespace PragmaticAnalyzer.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static void ReplaceAll<T>(this ObservableCollection<T> collection, IEnumerable<T> newItems)
        {
            collection.Clear();
            if (newItems != null)
            {
                foreach (var item in newItems)
                {
                    collection.Add(item);
                }
            }
        } // замена всех элементов коллекции
    } // расширения для ObservableCollection
}
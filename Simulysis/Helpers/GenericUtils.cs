using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;

using System.Threading.Tasks;
using Entities;

namespace Simulysis.Helpers
{
    public class GenericUtils
    {
        /*
         * <summary>
         *   Perform a deep copy of the object, using Json as a serialization method.
         *   NOTE: Object must be serializable. Private members are not cloned using this method.
         * </summary>
         * <typeparam name="T">The type of object being copied.</typeparam>
         * <param name="source">The object instance to copy.</param>
         * <returns>The copied object.</returns>
         */
        //public static T JsonDeepCopy<T>(T source)
        //{
        //    // Don't serialize a null object
        //    if (ReferenceEquals(source, null))
        //    {
        //        return default;
        //    }

        //    // initialize inner objects individually
        //    // for example in default constructor some list property initialized with some values,
        //    // but in 'source' these items are cleaned -
        //    // without ObjectCreationHandling.Replace default constructor values will be added to result
        //    var deserializeSettings =
        //        new JsonSerializerSettings {ObjectCreationHandling = ObjectCreationHandling.Replace};

        //    return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);
        //}

        public static List<List<T>> ChunkBy<T>(List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public static ConcurrentDictionary<K, List<V>> ToDictOfLists<K, V>(ConcurrentDictionary<K, ConcurrentBag<V>> dictionary)
        {
            ConcurrentDictionary<K, List<V>> dictOfLists = new ConcurrentDictionary<K, List<V>>();

            Parallel.ForEach(dictionary.Keys,
                new ParallelOptions { MaxDegreeOfParallelism = Configuration.MaxThreadNumber },
                key => dictOfLists.TryAdd(key, dictionary[key].ToList())
            );

            return dictOfLists;
        }

        public static string GetFileVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }
    }
}
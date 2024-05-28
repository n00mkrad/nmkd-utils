using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NmkdUtils
{
    public static class ObjectExtensions
    {
        /// <summary> Turns an object into an array of the same type with the object as its only entry </summary>
        public static T[] AsArray<T>(this T obj)
        {
            return new[] { obj };
        }

        /// <summary> Turns an object into a list of the same type with the object as its only entry </summary>
        public static List<T> AsList<T>(this T obj)
        {
            return new List<T> { obj };
        }

        /// <summary> Like Distinct() but on specified property </summary>
        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> items, Func<T, TKey> property)
        {
            return items.GroupBy(property).Select(x => x.First());
        }
    }
}

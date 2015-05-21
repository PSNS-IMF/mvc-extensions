using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Psns.Common.Mvc.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Gets all PropertyInfos of an object that implement IEnumerable
        /// </summary>
        /// <param name="obj">The object</param>
        /// <returns>IEnumerable<PropertyInfo></returns>
        public static IEnumerable<PropertyInfo> GetEnumerableProperties(this object obj)
        {
            return obj
                .GetType()
                .GetProperties()
                .Where(p => p.PropertyType.IsGenericType &&
                    p.PropertyType.GetInterfaces().Any(x => x == typeof(IEnumerable)));
        }
    }
}

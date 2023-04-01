using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public static class GlobalExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            while (source.Any())
            {
                yield return source.Take(batchSize);
                source = source.Skip(batchSize);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Vimium.Extensions
{
    public static class IEnumerableTExtensions
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            // Hint-label ordering is presentational, not security-sensitive, so a
            // fast pseudo-random generator is appropriate here.
#pragma warning disable SCS0005 // Weak random number generator
            var rnd = new Random();
            return source.OrderBy<T, int>((item) => rnd.Next());
#pragma warning restore SCS0005
        }
    }
}

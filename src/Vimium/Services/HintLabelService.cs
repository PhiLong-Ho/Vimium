using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vimium.Services.Interfaces;

namespace Vimium.Services
{
    internal class HintLabelService : IHintLabelService
    {
        /// <summary>
        /// The character set used to compose hint labels.
        /// 14 characters, ordered so that 'S' (index 0) is the most comfortable home-row key.
        /// </summary>
        /// <remarks>Adapted from vimium to give a consistent experience, see https://github.com/philc/vimium/blob/master/content_scripts/link_hints.js </remarks>
        private static readonly char[] HintCharacters =
            { 'S', 'A', 'D', 'F', 'J', 'K', 'L', 'E', 'W', 'C', 'M', 'P', 'G', 'H' };

        /// <summary>
        /// Pre-computed pools of hint strings keyed by length.
        /// Pool L contains all possible hint strings of exactly L characters (charsetSize^L entries).
        /// Lazy-initialized with double-checked locking for thread safety.;;
        /// </summary>
        private static readonly Dictionary<int, IReadOnlyList<string>> HintPools = new();
        private static readonly object PoolLock = new();

        /// <summary>
        /// Gets available hint strings. All returned hints have the same length,
        /// guaranteeing that no hint is a prefix of another — every hint is
        /// independently targetable.
        /// </summary>
        /// <param name="hintCount">The number of hints needed</param>
        /// <returns>A list of hint strings, all of uniform length</returns>
        public IList<string> GetHintStrings(int hintCount)
        {
            if (hintCount <= 0)
            {
                return Array.Empty<string>();
            }

            // Determine the minimum length L where charsetSize^L >= hintCount.
            // Using uniform-length hints guarantees no prefix relationship.
            var length = 1;
            var capacity = HintCharacters.Length;
            while (capacity < hintCount)
            {
                length++;
                capacity *= HintCharacters.Length;
            }

            var pool = GetOrCreatePool(length);
            return pool.Take(hintCount).ToList();
        }

        /// <summary>
        /// Returns the pre-computed pool for the given hint length,
        /// creating it on first access (thread-safe).
        /// </summary>
        private static IReadOnlyList<string> GetOrCreatePool(int length)
        {
            if (HintPools.TryGetValue(length, out var pool))
            {
                return pool;
            }

            lock (PoolLock)
            {
                if (HintPools.TryGetValue(length, out pool))
                {
                    return pool;
                }

                pool = GeneratePool(length);
                HintPools[length] = pool;
                return pool;
            }
        }

        /// <summary>
        /// Generates all possible hint strings of exactly <paramref name="length"/> characters
        /// in lexicographic (base-N counting) order.
        /// </summary>
        private static IReadOnlyList<string> GeneratePool(int length)
        {
            var count = (int)Math.Pow(HintCharacters.Length, length);
            var pool = new string[count];
            for (var i = 0; i < count; i++)
            {
                pool[i] = NumberToHintString(i, HintCharacters, length);
            }

            return pool;
        }

        /// <summary>
        /// Converts a number like "8" into a hint string like "JK". This is used to sequentially generate all of the
        /// hint text. The hint string will be "padded with zeroes" to ensure its length is >= numHintDigits.
        /// </summary>
        /// <remarks>Adapted from vimium to give a consistent experience, see https://github.com/philc/vimium/blob/master/content_scripts/link_hints.js</remarks>
        /// <param name="number">The number</param>
        /// <param name="characterSet">The set of characters</param>
        /// <param name="noHintDigits">The number of hint digits</param>
        /// <returns>A hint string</returns>
        private static string NumberToHintString(int number, char[] characterSet, int noHintDigits = 0)
        {
            var divisor = characterSet.Length;
            var hintString = new StringBuilder();

            do
            {
                var remainder = number % divisor;
                hintString.Insert(0, characterSet[remainder]);
                number -= remainder;
                number /= (int)Math.Floor((double)divisor);
            } while (number > 0);

            // Pad the hint string we're returning so that it matches numHintDigits.
            // Note: the loop body changes hintString.length, so the original length must be cached!
            var length = hintString.Length;
            for (var i = 0; i < (noHintDigits - length); ++i)
            {
                hintString.Insert(0, characterSet[0]);
            }

            return hintString.ToString();
        }
    }
}

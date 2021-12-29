using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Extensions
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Executes a foreach operation on an IEnumerable in which iterations run asynchronously.
        /// </summary>
        /// <typeparam name="TSource">Item type.</typeparam>
        /// <param name="collection">Enumerated collection.</param>
        /// <param name="maxDegreeOfParallelism">The maximum amount of items that can be processed simultaneously.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="action">Action that is used for processing each item in the collection.</param>
        public static async Task ForEachAsync<TSource>(this IEnumerable<TSource> collection, int maxDegreeOfParallelism, CancellationToken cancellationToken, Func<TSource, CancellationToken, Task> action)
        {
            var unfinished = new List<Task>();

            foreach (TSource item in collection)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (unfinished.Count >= maxDegreeOfParallelism)
                {
                    await Task.WhenAny(unfinished).ConfigureAwait(false);

                    foreach (Task toRemove in unfinished.Where(x => x.IsCompleted).ToList())
                        unfinished.Remove(toRemove);
                }

                if (!cancellationToken.IsCancellationRequested)
                    unfinished.Add(action(item, cancellationToken));
            }

            await Task.WhenAll(unfinished).ConfigureAwait(false);
        }

        /// <summary>
        /// Calculates a median value of a collection of long integers.
        /// </summary>
        /// <param name="source">Collection of numbers to count median of.</param>
        /// <returns>Median value, or 0 if the collection is empty.</returns>
        public static long Median(this IEnumerable<long> source)
        {
            int count = source.Count();
            if (count == 0)
                return 0;

            int midpoint = count / 2;
            IOrderedEnumerable<long> ordered = source.OrderBy(n => n);
            if ((count % 2) == 0)
                return (long)Math.Round(ordered.Skip(midpoint - 1).Take(2).Average());
            else
                return ordered.ElementAt(midpoint);
        }

        /// <summary>
        /// Calculates a median value of a collection of integers.
        /// </summary>
        /// <param name="source">Collection of numbers to count median of.</param>
        /// <returns>Median value, or 0 if the collection is empty.</returns>
        public static int Median(this IEnumerable<int> source)
        {
            int count = source.Count();
            if (count == 0)
                return 0;

            int midpoint = count / 2;
            IOrderedEnumerable<int> ordered = source.OrderBy(n => n);
            if ((count % 2) == 0)
                return (int)Math.Round(ordered.Skip(midpoint - 1).Take(2).Average());
            else
                return ordered.ElementAt(midpoint);
        }

        /// <summary>
        /// Calculates a median value of a collection of doubles.
        /// </summary>
        /// <param name="source">Collection of numbers to count median of.</param>
        /// <returns>Median value, or 0 if the collection is empty.</returns>
        public static double Median(this IEnumerable<double> source)
        {
            int count = source.Count();
            if (count == 0)
                return 0;

            int midpoint = count / 2;
            IOrderedEnumerable<double> ordered = source.OrderBy(n => n);
            if ((count % 2) == 0)
                return ordered.Skip(midpoint - 1).Take(2).Average();
            else
                return ordered.ElementAt(midpoint);
        }

        public static IEnumerable<List<T>> Partition<T>(this IEnumerable<T> source, int max)
        {
            return Partition(source, () => max);
        }

        public static IEnumerable<List<T>> Partition<T>(this IEnumerable<T> source, Func<int> max)
        {
            int partitionSize = max();
            var toReturn = new List<T>(partitionSize);
            foreach (T item in source)
            {
                toReturn.Add(item);
                if (toReturn.Count == partitionSize)
                {
                    yield return toReturn;
                    partitionSize = max();
                    toReturn = new List<T>(partitionSize);
                }
            }
            if (toReturn.Any())
            {
                yield return toReturn;
            }
        }
    }
}

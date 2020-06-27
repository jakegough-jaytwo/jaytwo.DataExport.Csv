using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.DataExport.Csv
{
    internal static class SemaphoreSlimExtensions
    {
        public static async Task RunAsync(this SemaphoreSlim semaphore, Func<Task> callback)
        {
            await semaphore.WaitAsync();
            try
            {
                await callback.Invoke();
            }
            finally
            {
                semaphore.Release();
            }
        }

        public static void Run(this SemaphoreSlim semaphore, Action callback)
        {
            semaphore.Wait();
            try
            {
                callback.Invoke();
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}

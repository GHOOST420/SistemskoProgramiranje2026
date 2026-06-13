using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Threading.Tasks;

namespace DrugiProjekatSisProg
{
    internal class Cache
    {
        private readonly IMemoryCache _cache;
        private static readonly ConcurrentDictionary<string, Task<string>> _inProgress = new();

        public Cache()
        {
            _cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 512
            });
        }

        public async Task<string?> GetOrAddAsync(string key, Func<Task<string>> factory)
        {
            if (_cache.TryGetValue(key, out string? cached))
            {
                Logger.Info($"Return value from cache for key: {key}");
                return cached;
            }

            var pending = _inProgress.GetOrAdd(key, _ => CreateAndCacheAsync(key, factory));
            try
            {
                return await pending;
            }
            finally
            {
                _inProgress.TryRemove(key, out _);
            }
        }

        private async Task<string> CreateAndCacheAsync(string key, Func<Task<string>> factory)
        {
            string result = await factory();

            _cache.Set(
                key,
                result,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(3),
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Size = 1
                });

            Logger.Info($"Cached value for key: {key}");
            return result;
        }
    }
}

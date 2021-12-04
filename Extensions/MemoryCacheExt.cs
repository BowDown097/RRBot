namespace RRBot.Extensions
{
    public static class MemoryCacheExt
    {
        public static void CacheDatabaseObject(this MemoryCache cache, string key, DbObject value)
        {
            CacheItemPolicy policy = new()
            {
                AbsoluteExpiration = DateTime.Now.AddMinutes(10.0),
                RemovedCallback = new CacheEntryRemovedCallback(CacheRemovedCallback)
            };

            cache.Add(key, value, policy);
        }

        public static void CacheRemovedCallback(CacheEntryRemovedArguments args)
        {
            DbObject item = (DbObject)args.CacheItem.Value;
            item.Reference.SetAsync(item);
        }
    }
}
namespace RRBot.Extensions;
public static class MemoryCacheExt
{
    public static void CacheDatabaseObject(this MemoryCache cache, string key, DbObject value)
    {
        CacheItemPolicy policy = new()
        {
            AbsoluteExpiration = DateTime.Now.AddMinutes(10.0),
            UpdateCallback = new CacheEntryUpdateCallback(CacheUpdateCallback)
        };

        cache.Set(key, value, policy);
    }

    private static void CacheUpdateCallback(CacheEntryUpdateArguments args)
    {
        DbObject item = (DbObject)MemoryCache.Default.Get(args.Key);
        item.Reference.SetAsync(item);
    }
}
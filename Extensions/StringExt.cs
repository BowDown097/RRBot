using System;
using System.Linq;

namespace RRBot.Extensions
{
    public static class StringExt
    {
        public static bool In(this string source, params string[] list)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return list.Contains(source);
        }
    }
}
using System;

namespace Home.Data.Helper
{
    public static class GeneralHelper
    {
        public static string FormatLogLine(this string content, DateTime time)
        {
            return $"[{time.ToShortDateString()} @ {time.ToShortTimeString()}]: {content}";
        }
    }
}

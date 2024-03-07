using System;

using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace com.nem.aurawheel.Utils
{
    class NemDateUtils
    {
        public static long UniversalTimeMillis(DateTime datetime)
        {
            TimeSpan start = new TimeSpan((new DateTime(1970, 1, 1)).Ticks);
            TimeSpan stop = new TimeSpan(TimeZone.CurrentTimeZone.ToUniversalTime(datetime).Ticks);
            return (long)stop.Subtract(start).TotalMilliseconds;
        }

        public static DateTime millis1970ToCurrentDate(long time)
        {
            long ticks = (long)(10000 * time);
            ticks += (new DateTime(1970, 1, 1)).Ticks;
            DateTime date = new DateTime(ticks);
            return TimeZone.CurrentTimeZone.ToLocalTime(date);
        }

        public static DateTime strDateToDatetime(String dateStr)
        {
            string pattern = "yyyy-MM-dd";
            return DateTime.ParseExact(dateStr, pattern, null);
        }

    }
}

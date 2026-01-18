namespace BusinessLogic.Helpers
{
    public static class TimeZoneHelper
    {
        public static DateTime ConvertToVietnamTime(DateTime utcTime)
        {
            try
            {
                var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, vietnamTimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
                    return TimeZoneInfo.ConvertTimeFromUtc(utcTime, vietnamTimeZone);
                }
                catch (TimeZoneNotFoundException)
                {
                    // Fallback: UTC+7 offset
                    return utcTime.AddHours(7);
                }
            }
        }
    }
}


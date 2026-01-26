using System;

namespace SmartTeam.Application.Helpers;

public static class TimeHelper
{
    private static readonly TimeZoneInfo _bakuTimeZone;

    static TimeHelper()
    {
        try
        {
            // Try standard Windows ID
            _bakuTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Azerbaijan Standard Time");
        }
        catch
        {
            try
            {
                // Try IANA ID (Linux/Docker)
                _bakuTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Baku");
            }
            catch
            {
                // Fallback to UTC+4 constant rule if system definitions missing
                _bakuTimeZone = TimeZoneInfo.CreateCustomTimeZone("Azerbaijan Standard Time", TimeSpan.FromHours(4), "Azerbaijan Standard Time", "Azerbaijan Standard Time");
            }
        }
    }

    /// <summary>
    /// Gets the current time in Baku (UTC+4)
    /// </summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _bakuTimeZone);

    /// <summary>
    /// Gets the Baku TimeZone info
    /// </summary>
    public static TimeZoneInfo BakuTimeZone => _bakuTimeZone;
    
    /// <summary>
    /// Converts a UTC DateTime to Baku Time
    /// </summary>
    public static DateTime ToBakuTime(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _bakuTimeZone);
    }
}

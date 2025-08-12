using System;

public static class CommonHelper
{
    public static string GetWindDirectionText(double degrees)
    {
        if (degrees < 0 || degrees > 360) return "N/A";

        string[] directions = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
                               "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
        int index = (int)((degrees + 11.25) / 22.5) % 16;
        return directions[index];
    }

    public static string GetUvIndexDescription(double uvIndex)
    {
        if (uvIndex <= 2) return "Low";
        if (uvIndex <= 5) return "Moderate";
        if (uvIndex <= 7) return "High";
        if (uvIndex <= 10) return "Very High";
        return "Extreme";
    }

    public static string GetWeatherDescription(int weatherCode)
    {
        return WeatherDescriptions.GetWeatherDescription(weatherCode);
    }

    public static string ConvertIso8601ToTime(string iso8601)
    {
        if (string.IsNullOrEmpty(iso8601)) return "N/A";

        try
        {
            DateTime dateTime = DateTime.Parse(iso8601, null, System.Globalization.DateTimeStyles.RoundtripKind);
            return dateTime.ToString("HH:mm");
        }
        catch
        {
            return "N/A";
        }
    }

    public static double ConvertIso8601ToHour(string iso8601)
    {
        if (string.IsNullOrEmpty(iso8601)) return 0.0;

        try
        {
            DateTime dateTime = DateTime.Parse(iso8601, null, System.Globalization.DateTimeStyles.RoundtripKind);
            return dateTime.Hour + (dateTime.Minute / 60.0);
        }
        catch
        {
            return 0.0;
        }
    }
    public static int GetTargetHourIndex(int hourOffset)
    {
        int currentHour = DateTime.Now.Hour;
        int targetIndex = currentHour + hourOffset;

        if (targetIndex >= 48)
        {
            targetIndex = 47;
        }

        return targetIndex;
    }

}
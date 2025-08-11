using System;

public static class WeatherDescriptions
{
    /// <summary>
    /// Converts Open-Meteo weather codes to human-readable descriptions
    /// Based on WMO Weather interpretation codes (WW)
    /// </summary>
    /// <param name="weatherCode">The weather code from Open-Meteo API</param>
    /// <returns>Human-readable weather description</returns>
    public static string GetWeatherDescription(int weatherCode)
    {
        return weatherCode switch
        {
            // Clear sky
            0 => "Clear Sky",

            // Mainly clear, partly cloudy, and overcast
            1 => "Mainly Clear",
            2 => "Partly Cloudy",
            3 => "Overcast",

            // Fog
            45 => "Fog",
            48 => "Depositing Rime Fog",

            // Drizzle
            51 => "Light Drizzle",
            53 => "Moderate Drizzle",
            55 => "Dense Drizzle",
            56 => "Light Freezing Drizzle",
            57 => "Dense Freezing Drizzle",

            // Rain
            61 => "Slight Rain",
            63 => "Moderate Rain",
            65 => "Heavy Rain",
            66 => "Light Freezing Rain",
            67 => "Heavy Freezing Rain",

            // Snow
            71 => "Slight Snow",
            73 => "Moderate Snow",
            75 => "Heavy Snow",
            77 => "Snow Grains",

            // Rain showers
            80 => "Slight Rain Showers",
            81 => "Moderate Rain Showers",
            82 => "Violent Rain Showers",

            // Snow showers
            85 => "Slight Snow Showers",
            86 => "Heavy Snow Showers",

            // Thunderstorms
            95 => "Thunderstorm",
            96 => "Thunderstorm with Slight Hail",
            99 => "Thunderstorm with Heavy Hail",

            // Unknown/Invalid codes
            _ => "Unknown"
        };
    }
}
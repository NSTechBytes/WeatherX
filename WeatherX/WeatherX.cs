using Rainmeter;
using System.Runtime.InteropServices;
using System;
using System.Net;
using System.IO;
using System.Text;
using System.Globalization;

internal class Measure
{
    private double latitude, longitude;
    private string dataType;
    private int forecastDay;
    private int updateInterval;
    private DateTime lastUpdate;
    private API api;

    // Simple weather data storage
    private double currentTemp = 0.0;
    private string currentCondition = "Unknown";
    private double currentHumidity = 0.0;
    private double currentWindSpeed = 0.0;
    private double currentPressure = 0.0;

    // Simple forecast arrays (7 days max)
    private double[] forecastTempMax = new double[7];
    private double[] forecastTempMin = new double[7];
    private string[] forecastConditions = new string[7];

    // Debug info
    private string lastError = "";
    private string lastApiUrl = "";

    internal Measure()
    {
        latitude = 0.0;
        longitude = 0.0;
        dataType = "CurrentTemp";
        forecastDay = 0;
        updateInterval = 600; // 10 minutes default
        lastUpdate = DateTime.MinValue;

        // Initialize forecast arrays
        for (int i = 0; i < 7; i++)
        {
            forecastTempMax[i] = 0.0;
            forecastTempMin[i] = 0.0;
            forecastConditions[i] = "Unknown";
        }
    }

    internal void Reload(Rainmeter.API api, ref double maxValue)
    {
        this.api = api;

        latitude = api.ReadDouble("Latitude", 0.0);
        longitude = api.ReadDouble("Longitude", 0.0);
        dataType = api.ReadString("DataType", "CurrentTemp");
        forecastDay = api.ReadInt("ForecastDay", 0);
        updateInterval = api.ReadInt("UpdateInterval", 600);

        // Validate coordinates
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            lastError = "Invalid coordinates";
            api.Log(API.LogType.Error, $"WeatherX: Invalid coordinates Lat={latitude}, Lon={longitude}");
        }
        else
        {
            lastError = "";
        }

        // Validate forecast day
        if (forecastDay < 0 || forecastDay > 6)
        {
            forecastDay = 0;
        }

        // Force initial update
        lastUpdate = DateTime.MinValue;

        api.Log(API.LogType.Debug, $"WeatherX: Initialized with Lat={latitude}, Lon={longitude}, DataType={dataType}");
    }

    internal double Update()
    {
        // Check if update is needed
        if (DateTime.Now.Subtract(lastUpdate).TotalSeconds >= updateInterval)
        {
            UpdateWeatherData();
        }

        return GetNumericValue();
    }

    private void UpdateWeatherData()
    {
        try
        {
            lastApiUrl = BuildApiUrl();
            api?.Log(API.LogType.Debug, $"WeatherX: Making API call to: {lastApiUrl}");

            // Enable TLS 1.2 for SSL connections
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 |
                System.Net.SecurityProtocolType.Tls11 |
                System.Net.SecurityProtocolType.Tls;

            // Use WebClient for simple HTTP requests (no external dependencies)
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent", "WeatherX-Rainmeter-Plugin/1.0");
                client.Headers.Add("Accept", "application/json");
                client.Encoding = Encoding.UTF8;

                string response = client.DownloadString(lastApiUrl);
                ParseWeatherData(response);

                lastError = "";
                lastUpdate = DateTime.Now;
                api?.Log(API.LogType.Debug, $"WeatherX: Successfully updated weather data. Temp: {currentTemp}");
            }
        }
        catch (WebException ex)
        {
            // Try HTTP fallback if HTTPS fails
            if (lastApiUrl.StartsWith("https://"))
            {
                try
                {
                    api?.Log(API.LogType.Warning, $"WeatherX: HTTPS failed, trying HTTP fallback");
                    string httpUrl = lastApiUrl.Replace("https://", "http://");

                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("User-Agent", "WeatherX-Rainmeter-Plugin/1.0");
                        client.Headers.Add("Accept", "application/json");
                        client.Encoding = Encoding.UTF8;

                        string response = client.DownloadString(httpUrl);
                        ParseWeatherData(response);

                        lastError = "";
                        lastUpdate = DateTime.Now;
                        api?.Log(API.LogType.Debug, $"WeatherX: HTTP fallback successful. Temp: {currentTemp}");
                        return;
                    }
                }
                catch (Exception fallbackEx)
                {
                    api?.Log(API.LogType.Error, $"WeatherX: HTTP fallback also failed: {fallbackEx.Message}");
                }
            }

            lastError = $"Network Error: {ex.Message}";
            api?.Log(API.LogType.Error, $"WeatherX: {lastError}");
        }
        catch (Exception ex)
        {
            lastError = $"Error: {ex.Message}";
            api?.Log(API.LogType.Error, $"WeatherX: {lastError}");
        }
    }

    private string BuildApiUrl()
    {
        return $"https://api.open-meteo.com/v1/forecast?" +
               $"latitude={latitude.ToString(CultureInfo.InvariantCulture)}&" +
               $"longitude={longitude.ToString(CultureInfo.InvariantCulture)}&" +
               $"current=temperature_2m,relative_humidity_2m,weather_code,surface_pressure,wind_speed_10m&" +
               $"daily=weather_code,temperature_2m_max,temperature_2m_min&" +
               $"forecast_days=7&" +
               $"timezone=auto";
    }

    private void ParseWeatherData(string jsonResponse)
    {
        try
        {
            // Simple JSON parsing without external libraries
            // Parse current temperature
            currentTemp = JsonParser.ParseJsonValue(jsonResponse, "\"current\"", "\"temperature_2m\"");

            // Parse current humidity
            currentHumidity = JsonParser.ParseJsonValue(jsonResponse, "\"current\"", "\"relative_humidity_2m\"");

            // Parse current wind speed
            currentWindSpeed = JsonParser.ParseJsonValue(jsonResponse, "\"current\"", "\"wind_speed_10m\"");

            // Parse current pressure
            currentPressure = JsonParser.ParseJsonValue(jsonResponse, "\"current\"", "\"surface_pressure\"");

            // Parse current weather code
            int weatherCode = (int)JsonParser.ParseJsonValue(jsonResponse, "\"current\"", "\"weather_code\"");
            currentCondition = GetWeatherDescription(weatherCode);

            // Parse daily forecasts
            ParseDailyForecasts(jsonResponse);

            api?.Log(API.LogType.Debug, $"WeatherX: Parsed - Temp: {currentTemp}, Condition: {currentCondition}");
        }
        catch (Exception ex)
        {
            api?.Log(API.LogType.Error, $"WeatherX: JSON parsing error: {ex.Message}");
            throw;
        }
    }

    private void ParseDailyForecasts(string json)
    {
        try
        {
            // Find daily section
            int dailyStart = json.IndexOf("\"daily\"");
            if (dailyStart == -1) return;

            // Parse temperature_2m_max array
            ParseArrayValues(json, "\"temperature_2m_max\"", forecastTempMax);

            // Parse temperature_2m_min array
            ParseArrayValues(json, "\"temperature_2m_min\"", forecastTempMin);

            // Parse weather codes and convert to conditions
            double[] weatherCodes = new double[7];
            ParseArrayValues(json, "\"weather_code\"", weatherCodes);

            for (int i = 0; i < 7; i++)
            {
                forecastConditions[i] = GetWeatherDescription((int)weatherCodes[i]);
            }
        }
        catch (Exception ex)
        {
            api?.Log(API.LogType.Warning, $"WeatherX: Daily forecast parsing error: {ex.Message}");
        }
    }

    private void ParseArrayValues(string json, string arrayName, double[] outputArray)
    {
        try
        {
            int arrayStart = json.IndexOf(arrayName);
            if (arrayStart == -1) return;

            int bracketStart = json.IndexOf("[", arrayStart);
            if (bracketStart == -1) return;

            int bracketEnd = json.IndexOf("]", bracketStart);
            if (bracketEnd == -1) return;

            string arrayContent = json.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
            string[] values = arrayContent.Split(',');

            for (int i = 0; i < Math.Min(values.Length, outputArray.Length); i++)
            {
                string value = values[i].Trim();
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                {
                    outputArray[i] = result;
                }
            }
        }
        catch
        {
            // Ignore parsing errors for individual arrays
        }
    }

    private double GetNumericValue()
    {
        switch (dataType.ToLower())
        {
            case "currenttemp":
                return currentTemp;
            case "currenthumidity":
                return currentHumidity;
            case "currentwindspeed":
                return currentWindSpeed;
            case "currentpressure":
                return currentPressure;
            case "forecasttemp":
            case "forecasttempmax":
                return forecastDay < forecastTempMax.Length ? forecastTempMax[forecastDay] : 0.0;
            case "forecasttempmin":
                return forecastDay < forecastTempMin.Length ? forecastTempMin[forecastDay] : 0.0;
            default:
                return 0.0;
        }
    }

    internal string GetStringValue()
    {
        switch (dataType.ToLower())
        {
            case "currentcondition":
                return currentCondition;
            case "forecastcondition":
                return forecastDay < forecastConditions.Length ? forecastConditions[forecastDay] : "Unknown";
            case "debugerror":
                return string.IsNullOrEmpty(lastError) ? "No Error" : lastError;
            case "debugurl":
                return lastApiUrl;
            default:
                double value = GetNumericValue();
                return value.ToString("F1", CultureInfo.InvariantCulture);
        }
    }

    private string GetWeatherDescription(int weatherCode)
    {
        return WeatherDescriptions.GetWeatherDescription(weatherCode);
    }
}
using Rainmeter;
using System;
using System.Net;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

public class TimeoutWebClient : WebClient
{
    public int Timeout { get; set; } = 10000;

    protected override WebRequest GetWebRequest(Uri address)
    {
        WebRequest request = base.GetWebRequest(address);
        request.Timeout = Timeout;
        return request;
    }
}

internal class Measure
{
    private double latitude, longitude;
    private string dataType;
    private int forecastDay;
    private int hourOffset;
    private int updateInterval;
    private DateTime lastUpdate;
    private API api;
    private string units;
    private string timezone;

    // Current weather data (from current section)
    private double currentTemp = 0.0;
    private string currentCondition = "Unknown";
    private double currentHumidity = 0.0;
    private double currentWindSpeed = 0.0;
    private double currentPressure = 0.0;
    private double currentApparentTemp = 0.0;
    private double currentDewPoint = 0.0;
    private double currentCloudCover = 0.0;
    private double currentWindDirection = 0.0;
    private double currentWindGusts = 0.0;

    // Solar radiation data (from hourly data)
    private double currentSolarRadiation = 0.0;
    private double currentDirectRadiation = 0.0;
    private double currentDiffuseRadiation = 0.0;

    private double todayUvIndex = 0.0;

    // Daily forecast arrays (removed precipitation-related)
    private double[] forecastTempMax = new double[7];
    private double[] forecastTempMin = new double[7];
    private double[] forecastApparentTempMax = new double[7];
    private double[] forecastApparentTempMin = new double[7];
    private string[] forecastConditions = new string[7];
    private double[] forecastWindSpeedMax = new double[7];
    private double[] forecastUvIndexMax = new double[7];
    private string[] forecastSunrise = new string[7];
    private string[] forecastSunset = new string[7];

    // Hourly arrays for 48 hours (removed precipitation-related)
    private double[] hourlyTemp = new double[48];
    private double[] hourlyHumidity = new double[48];
    private double[] hourlyWindSpeed = new double[48];
    private double[] hourlyApparentTemp = new double[48];
    private double[] hourlyCloudCover = new double[48];
    private double[] hourlyVisibility = new double[48];
    private int[] hourlyWeatherCode = new int[48];
    private string[] hourlyTime = new string[48];

    // Solar radiation arrays for hourly data
    private double[] hourlySolarRadiation = new double[48];
    private double[] hourlyDirectRadiation = new double[48];
    private double[] hourlyDiffuseRadiation = new double[48];

    private string lastError = "";
    private string lastApiUrl = "";
    private volatile bool isUpdating = false;
    private bool initialUpdateScheduled = false;
    private Timer updateTimer;

    internal Measure()
    {
        latitude = 0.0;
        longitude = 0.0;
        dataType = "CurrentTemp";
        forecastDay = 0;
        hourOffset = 0;
        updateInterval = 600;
        lastUpdate = DateTime.MinValue;
        units = "metric";
        timezone = "auto";

        InitializeArrays();
    }

    private void InitializeArrays()
    {
        // Daily forecast arrays
        for (int i = 0; i < 7; i++)
        {
            forecastTempMax[i] = 0.0;
            forecastTempMin[i] = 0.0;
            forecastApparentTempMax[i] = 0.0;
            forecastApparentTempMin[i] = 0.0;
            forecastConditions[i] = "Unknown";
            forecastWindSpeedMax[i] = 0.0;
            forecastUvIndexMax[i] = 0.0;
            forecastSunrise[i] = "";
            forecastSunset[i] = "";
        }

        // Hourly arrays
        for (int i = 0; i < 48; i++)
        {
            hourlyTemp[i] = 0.0;
            hourlyHumidity[i] = 0.0;
            hourlyWindSpeed[i] = 0.0;
            hourlyApparentTemp[i] = 0.0;
            hourlyCloudCover[i] = 0.0;
            hourlyVisibility[i] = 0.0;
            hourlyWeatherCode[i] = 0;
            hourlyTime[i] = "";
            hourlySolarRadiation[i] = 0.0;
            hourlyDirectRadiation[i] = 0.0;
            hourlyDiffuseRadiation[i] = 0.0;
        }
    }

    internal void Reload(Rainmeter.API api, ref double maxValue)
    {
        this.api = api;

        latitude = api.ReadDouble("Latitude", 0.0);
        longitude = api.ReadDouble("Longitude", 0.0);
        dataType = api.ReadString("DataType", "CurrentTemp");
        forecastDay = api.ReadInt("ForecastDay", 0);
        hourOffset = api.ReadInt("HourOffset", 0);
        updateInterval = api.ReadInt("UpdateInterval", 600);
        units = api.ReadString("Units", "metric").ToLower();
        timezone = api.ReadString("Timezone", "auto");

        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            lastError = "Invalid coordinates";
            api.Log(API.LogType.Error, $"WeatherX: Invalid coordinates Lat={latitude}, Lon={longitude}");
        }
        else
        {
            lastError = "";
        }

        if (forecastDay < 0 || forecastDay > 6)
        {
            forecastDay = 0;
        }

        if (hourOffset < 0 || hourOffset > 47)
        {
            hourOffset = 0;
        }

        if (units != "metric" && units != "imperial")
        {
            units = "metric";
        }

        lastUpdate = DateTime.MinValue;
        initialUpdateScheduled = false;
        isUpdating = false;

        updateTimer?.Dispose();

        api.Log(API.LogType.Debug, $"WeatherX: Initialized with Lat={latitude}, Lon={longitude}, DataType={dataType}, Units={units}, HourOffset={hourOffset}");
    }

    internal double Update()
    {
        if (!initialUpdateScheduled && string.IsNullOrEmpty(lastError))
        {
            initialUpdateScheduled = true;
            updateTimer = new Timer(async _ => await UpdateWeatherDataAsync(), null, 2000, Timeout.Infinite);
        }
        else if (DateTime.Now.Subtract(lastUpdate).TotalSeconds >= updateInterval && !isUpdating)
        {
            Task.Run(async () => await UpdateWeatherDataAsync());
        }

        return GetNumericValue();
    }

    private async Task UpdateWeatherDataAsync()
    {
        if (isUpdating) return;

        isUpdating = true;

        try
        {
            lastApiUrl = BuildApiUrl();
            api?.Log(API.LogType.Debug, $"WeatherX: Making API call to: {lastApiUrl}");

            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 |
                System.Net.SecurityProtocolType.Tls11 |
                System.Net.SecurityProtocolType.Tls;

            using (TimeoutWebClient client = new TimeoutWebClient())
            {
                client.Headers.Add("User-Agent", "WeatherX-Rainmeter-Plugin/2.2");
                client.Headers.Add("Accept", "application/json");
                client.Encoding = Encoding.UTF8;
                client.Timeout = 15000;

                string response = await client.DownloadStringTaskAsync(lastApiUrl);
                ParseWeatherData(response);

                lastError = "";
                lastUpdate = DateTime.Now;
            }
        }
        catch (WebException ex)
        {
            if (lastApiUrl.StartsWith("https://"))
            {
                try
                {
                    api?.Log(API.LogType.Warning, $"WeatherX: HTTPS failed, trying HTTP fallback");
                    string httpUrl = lastApiUrl.Replace("https://", "http://");

                    using (TimeoutWebClient client = new TimeoutWebClient())
                    {
                        client.Headers.Add("User-Agent", "WeatherX-Rainmeter-Plugin/2.2");
                        client.Headers.Add("Accept", "application/json");
                        client.Encoding = Encoding.UTF8;
                        client.Timeout = 15000;

                        string response = await client.DownloadStringTaskAsync(httpUrl);
                        ParseWeatherData(response);

                        lastError = "";
                        lastUpdate = DateTime.Now;
                        api?.Log(API.LogType.Debug, $"WeatherX: HTTP fallback successful. Temp: {currentTemp}°");
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
        finally
        {
            isUpdating = false;

            if (updateTimer != null)
            {
                updateTimer.Change(updateInterval * 1000, Timeout.Infinite);
            }
        }
    }

    private string BuildApiUrl()
    {
        string tempUnit = units == "imperial" ? "fahrenheit" : "celsius";
        string windUnit = units == "imperial" ? "mph" : "kmh";

        return $"https://api.open-meteo.com/v1/forecast?" +
               $"latitude={latitude.ToString(CultureInfo.InvariantCulture)}&" +
               $"longitude={longitude.ToString(CultureInfo.InvariantCulture)}&" +
               // Current weather (no precipitation)
               $"current=temperature_2m,relative_humidity_2m,weather_code,surface_pressure,wind_speed_10m," +
               $"apparent_temperature,dew_point_2m,wind_direction_10m,wind_gusts_10m&" +
               // Hourly parameters (no precipitation)
               $"hourly=temperature_2m,relative_humidity_2m,wind_speed_10m," +
               $"apparent_temperature,cloud_cover,visibility,weather_code," +
               $"shortwave_radiation,direct_radiation,diffuse_radiation&" +
               // Daily parameters (no precipitation)
               $"daily=weather_code,temperature_2m_max,temperature_2m_min,apparent_temperature_max," +
               $"apparent_temperature_min,wind_speed_10m_max,uv_index_max," +
               $"sunrise,sunset,shortwave_radiation_sum&" +
               $"temperature_unit={tempUnit}&" +
               $"wind_speed_unit={windUnit}&" +
               $"forecast_days=7&" +
               $"forecast_hours=48&" +
               $"timezone={timezone}";
    }

    public void ParseWeatherData(string jsonResponse)
    {
        try
        {
            ParseCurrentWeather(jsonResponse);
            ParseDailyForecasts(jsonResponse);
            ParseHourlyData(jsonResponse);

            // Update current weather from hourly data for solar radiation
            UpdateCurrentFromHourly();

            api?.Log(API.LogType.Debug, $"WeatherX: Comprehensive parsing complete - Temp: {currentTemp}°, Condition: {currentCondition}");
        }
        catch (Exception ex)
        {
            api?.Log(API.LogType.Error, $"WeatherX: JSON parsing error: {ex.Message}");
            throw;
        }
    }

    private void ParseCurrentWeather(string json)
    {
        // Parse current weather from "current" section
        currentTemp = ParseHelper.ParseJsonValue(json, "\"current\"", "\"temperature_2m\"");
        currentHumidity = ParseHelper.ParseJsonValue(json, "\"current\"", "\"relative_humidity_2m\"");
        currentWindSpeed = ParseHelper.ParseJsonValue(json, "\"current\"", "\"wind_speed_10m\"");
        currentPressure = ParseHelper.ParseJsonValue(json, "\"current\"", "\"surface_pressure\"");

        currentApparentTemp = ParseHelper.ParseJsonValue(json, "\"current\"", "\"apparent_temperature\"");
        currentDewPoint = ParseHelper.ParseJsonValue(json, "\"current\"", "\"dew_point_2m\"");
        currentWindDirection = ParseHelper.ParseJsonValue(json, "\"current\"", "\"wind_direction_10m\"");
        currentWindGusts = ParseHelper.ParseJsonValue(json, "\"current\"", "\"wind_gusts_10m\"");

        int weatherCode = (int)ParseHelper.ParseJsonValue(json, "\"current\"", "\"weather_code\"");
        currentCondition = CommonHelper.GetWeatherDescription(weatherCode);
    }

    private void UpdateCurrentFromHourly()
    {
        // Get current hour data from hourly arrays for values not available in current section
        int currentHourIndex = DateTime.Now.Hour;
        if (currentHourIndex < 48)
        {
            currentCloudCover = hourlyCloudCover[currentHourIndex];
            currentSolarRadiation = hourlySolarRadiation[currentHourIndex];
            currentDirectRadiation = hourlyDirectRadiation[currentHourIndex];
            currentDiffuseRadiation = hourlyDiffuseRadiation[currentHourIndex];
        }
    }

    private void ParseDailyForecasts(string json)
    {
        try
        {
            int dailyStart = json.IndexOf("\"daily\"");
            if (dailyStart == -1)
            {
                api?.Log(API.LogType.Warning, "WeatherX: Daily section not found in JSON");
                return;
            }

            string dailySection = ParseHelper.ExtractDailySection(json, dailyStart);

            ParseHelper.ParseArrayValuesInSection(dailySection, "\"temperature_2m_max\"", forecastTempMax);
            ParseHelper.ParseArrayValuesInSection(dailySection, "\"temperature_2m_min\"", forecastTempMin);
            ParseHelper.ParseArrayValuesInSection(dailySection, "\"apparent_temperature_max\"", forecastApparentTempMax);
            ParseHelper.ParseArrayValuesInSection(dailySection, "\"apparent_temperature_min\"", forecastApparentTempMin);
            ParseHelper.ParseArrayValuesInSection(dailySection, "\"wind_speed_10m_max\"", forecastWindSpeedMax);
            ParseHelper.ParseArrayValuesInSection(dailySection, "\"uv_index_max\"", forecastUvIndexMax);

            ParseHelper.ParseStringArrayInSection(dailySection, "\"sunrise\"", forecastSunrise);
            ParseHelper.ParseStringArrayInSection(dailySection, "\"sunset\"", forecastSunset);

            double[] weatherCodes = new double[7];
            ParseHelper.ParseArrayValuesInSection(dailySection, "\"weather_code\"", weatherCodes);

            for (int i = 0; i < 7; i++)
            {
                forecastConditions[i] = CommonHelper.GetWeatherDescription((int)weatherCodes[i]);
            }

            if (forecastUvIndexMax.Length > 0)
            {
                todayUvIndex = forecastUvIndexMax[0];
            }

            api?.Log(API.LogType.Debug, $"WeatherX: Daily forecasts parsed - Day 0: Max={forecastTempMax[0]}°, UV={forecastUvIndexMax[0]}");
        }
        catch (Exception ex)
        {
            api?.Log(API.LogType.Warning, $"WeatherX: Daily forecast parsing error: {ex.Message}");
        }
    }

    private void ParseHourlyData(string json)
    {
        try
        {
            int hourlyStart = json.IndexOf("\"hourly\"");
            if (hourlyStart == -1) return;

            string hourlySection = ParseHelper.ExtractHourlySection(json, hourlyStart);

            // Parse all hourly data (up to 48 hours) - no precipitation
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"temperature_2m\"", hourlyTemp, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"relative_humidity_2m\"", hourlyHumidity, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"wind_speed_10m\"", hourlyWindSpeed, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"apparent_temperature\"", hourlyApparentTemp, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"cloud_cover\"", hourlyCloudCover, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"visibility\"", hourlyVisibility, 48);

            // Parse solar radiation data from hourly section
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"shortwave_radiation\"", hourlySolarRadiation, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"direct_radiation\"", hourlyDirectRadiation, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"diffuse_radiation\"", hourlyDiffuseRadiation, 48);

            // Parse weather codes for hourly conditions
            double[] weatherCodes = new double[48];
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"weather_code\"", weatherCodes, 48);
            for (int i = 0; i < 48; i++)
            {
                hourlyWeatherCode[i] = (int)weatherCodes[i];
            }

            ParseHelper.ParseTimeArray(hourlySection, hourlyTime);

            api?.Log(API.LogType.Debug, $"WeatherX: Hourly data parsed successfully (no precipitation)");

        }
        catch (Exception ex)
        {
            api?.Log(API.LogType.Warning, $"WeatherX: Hourly data parsing error: {ex.Message}");
        }
    }

    private double GetNumericValue()
    {
        int targetHour = CommonHelper.GetTargetHourIndex(hourOffset);

        switch (dataType.ToLower())
        {
            // Current weather data
            case "currenttemp":
                return currentTemp;
            case "currenthumidity":
                return currentHumidity;
            case "currentwindspeed":
                return currentWindSpeed;
            case "currentpressure":
                return currentPressure;
            case "currentapparenttemp":
                return currentApparentTemp;
            case "currentdewpoint":
                return currentDewPoint;
            case "currentcloudcover":
                return currentCloudCover;
            case "currentwinddirection":
                return currentWindDirection;
            case "currentwindgusts":
                return currentWindGusts;
            case "currentuvindex":
                return todayUvIndex;

            // Current solar radiation from hourly data
            case "currentsolarradiation":
                return currentSolarRadiation;
            case "currentdirectradiation":
                return currentDirectRadiation;
            case "currentdiffuseradiation":
                return currentDiffuseRadiation;

            // Daily forecast data (no precipitation)
            case "forecasttemp":
            case "forecasttempmax":
                return forecastDay < forecastTempMax.Length ? forecastTempMax[forecastDay] : 0.0;
            case "forecasttempmin":
                return forecastDay < forecastTempMin.Length ? forecastTempMin[forecastDay] : 0.0;
            case "forecastapparenttempmax":
                return forecastDay < forecastApparentTempMax.Length ? forecastApparentTempMax[forecastDay] : 0.0;
            case "forecastapparenttempmin":
                return forecastDay < forecastApparentTempMin.Length ? forecastApparentTempMin[forecastDay] : 0.0;
            case "forecastwindspeed":
                return forecastDay < forecastWindSpeedMax.Length ? forecastWindSpeedMax[forecastDay] : 0.0;
            case "forecastuvindex":
                return forecastDay < forecastUvIndexMax.Length ? forecastUvIndexMax[forecastDay] : 0.0;
            case "forecastsunrise":
                return forecastDay < forecastSunrise.Length ? CommonHelper.ConvertIso8601ToHour(forecastSunrise[forecastDay]) : 0.0;
            case "forecastsunset":
                return forecastDay < forecastSunset.Length ? CommonHelper.ConvertIso8601ToHour(forecastSunset[forecastDay]) : 0.0;

            // Hourly data (no precipitation)
            case "hourlytemp":
                return targetHour < hourlyTemp.Length ? hourlyTemp[targetHour] : 0.0;
            case "hourlyhumidity":
                return targetHour < hourlyHumidity.Length ? hourlyHumidity[targetHour] : 0.0;
            case "hourlywindspeed":
                return targetHour < hourlyWindSpeed.Length ? hourlyWindSpeed[targetHour] : 0.0;
            case "hourlyapparenttemp":
                return targetHour < hourlyApparentTemp.Length ? hourlyApparentTemp[targetHour] : 0.0;
            case "hourlycloudcover":
                return targetHour < hourlyCloudCover.Length ? hourlyCloudCover[targetHour] : 0.0;
            case "hourlyvisibility":
                return targetHour < hourlyVisibility.Length ? hourlyVisibility[targetHour] : 0.0;

            // Hourly solar radiation
            case "hourlysolarradiation":
                return targetHour < hourlySolarRadiation.Length ? hourlySolarRadiation[targetHour] : 0.0;
            case "hourlydirectradiation":
                return targetHour < hourlyDirectRadiation.Length ? hourlyDirectRadiation[targetHour] : 0.0;
            case "hourlydiffuseradiation":
                return targetHour < hourlyDiffuseRadiation.Length ? hourlyDiffuseRadiation[targetHour] : 0.0;

            // Legacy compatibility (uses current hour)
            case "currenthourtemp":
                return hourlyTemp[DateTime.Now.Hour % 48];
            case "currenthourhumidity":
                return hourlyHumidity[DateTime.Now.Hour % 48];
            case "currenthourwindspeed":
                return hourlyWindSpeed[DateTime.Now.Hour % 48];

            default:
                return 0.0;
        }
    }

    internal string GetStringValue()
    {
        int targetHour = CommonHelper.GetTargetHourIndex(hourOffset);

        switch (dataType.ToLower())
        {
            case "currentcondition":
                return currentCondition;
            case "forecastcondition":
                return forecastDay < forecastConditions.Length ? forecastConditions[forecastDay] : "Unknown";
            case "hourlycondition":
                return targetHour < hourlyWeatherCode.Length ? CommonHelper.GetWeatherDescription(hourlyWeatherCode[targetHour]) : "Unknown";
            case "currentwinddirectiontext":
                return CommonHelper.GetWindDirectionText(currentWindDirection);
            case "debugerror":
                return string.IsNullOrEmpty(lastError) ? "No Error" : lastError;
            case "debugurl":
                return lastApiUrl;
            case "debugdaily":
                return $"Day{forecastDay}: Max={forecastTempMax[forecastDay]:F1}°, Min={forecastTempMin[forecastDay]:F1}°, {forecastConditions[forecastDay]}";
            case "debughourly":
                return $"Hour+{hourOffset}: Temp={hourlyTemp[targetHour]:F1}°, {CommonHelper.GetWeatherDescription(hourlyWeatherCode[targetHour])}";
            case "status":
                return isUpdating ? "Updating..." : (lastError.Length > 0 ? "Error" : "Ready");
            case "forecastsunrisetext":
                return forecastDay < forecastSunrise.Length ? CommonHelper.ConvertIso8601ToTime(forecastSunrise[forecastDay]) : "N/A";
            case "forecastsunsettext":
                return forecastDay < forecastSunset.Length ? CommonHelper.ConvertIso8601ToTime(forecastSunset[forecastDay]) : "N/A";
            case "hourlytime":
                return targetHour < hourlyTime.Length ? hourlyTime[targetHour] : "";
            case "currenthourlytime":
                {
                    int currentHourIndex = DateTime.Now.Hour % 48;
                    return currentHourIndex < hourlyTime.Length ? hourlyTime[currentHourIndex] : "";
                }
            case "uvindextext":
                return CommonHelper.GetUvIndexDescription(todayUvIndex);
            case "nexthourssummary":
                return GetNextHoursSummary();

            // Debug information
            case "debugsolarradiation":
                return $"Solar: {currentSolarRadiation:F1} W/m² | Direct: {currentDirectRadiation:F1} | Diffuse: {currentDiffuseRadiation:F1}";
            case "debugcloudcover":
                return $"Current Clouds: {currentCloudCover:F0}% | Next hour: {(targetHour < hourlyCloudCover.Length ? hourlyCloudCover[targetHour].ToString("F0") + "%" : "N/A")}";

            default:
                double value = GetNumericValue();
                return value.ToString("F1", CultureInfo.InvariantCulture);
        }
    }

    private string GetNextHoursSummary()
    {
        var summary = new StringBuilder();
        int currentHour = DateTime.Now.Hour;

        for (int i = 1; i <= Math.Min(6, 47 - currentHour); i++)
        {
            int hourIndex = currentHour + i;
            if (hourIndex < hourlyTemp.Length)
            {
                string time = hourIndex < hourlyTime.Length ?
                    CommonHelper.ConvertIso8601ToTime(hourlyTime[hourIndex]) :
                    DateTime.Now.AddHours(i).ToString("HH:mm");

                summary.Append($"{time}: {hourlyTemp[hourIndex]:F0}°");

                // Add condition to summary
                if (hourIndex < hourlyWeatherCode.Length)
                {
                    string condition = CommonHelper.GetWeatherDescription(hourlyWeatherCode[hourIndex]);
                    if (!condition.Equals("Clear Sky", StringComparison.OrdinalIgnoreCase))
                    {
                        summary.Append($" ({condition})");
                    }
                }

                if (i < Math.Min(6, 47 - currentHour))
                {
                    summary.Append(" | ");
                }
            }
        }

        return summary.ToString();
    }

    ~Measure()
    {
        updateTimer?.Dispose();
    }
}
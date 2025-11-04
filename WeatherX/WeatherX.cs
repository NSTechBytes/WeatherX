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

// Shared weather data cache that all measures can access
internal static class WeatherDataCache
{
    public static double currentTemp = 0.0;
    public static string currentCondition = "Unknown";
    public static double currentHumidity = 0.0;
    public static double currentWindSpeed = 0.0;
    public static double currentPressure = 0.0;
    public static double currentApparentTemp = 0.0;
    public static double currentDewPoint = 0.0;
    public static double currentCloudCover = 0.0;
    public static double currentWindDirection = 0.0;
    public static double currentWindGusts = 0.0;
    public static double currentSolarRadiation = 0.0;
    public static double currentDirectRadiation = 0.0;
    public static double currentDiffuseRadiation = 0.0;
    public static double currentIsDay = 0.0;
    public static double currentWeatherCode = 0.0;
    public static double todayUvIndex = 0.0;

    public static double[] forecastTempMax = new double[7];
    public static double[] forecastTempMin = new double[7];
    public static double[] forecastApparentTempMax = new double[7];
    public static double[] forecastApparentTempMin = new double[7];
    public static string[] forecastConditions = new string[7];
    public static double[] forecastWeatherCode = new double[7];
    public static double[] forecastWindSpeedMax = new double[7];
    public static double[] forecastUvIndexMax = new double[7];
    public static string[] forecastSunrise = new string[7];
    public static string[] forecastSunset = new string[7];

    public static double[] hourlyTemp = new double[48];
    public static double[] hourlyHumidity = new double[48];
    public static double[] hourlyWindSpeed = new double[48];
    public static double[] hourlyApparentTemp = new double[48];
    public static double[] hourlyCloudCover = new double[48];
    public static double[] hourlyVisibility = new double[48];
    public static int[] hourlyWeatherCode = new int[48];
    public static string[] hourlyTime = new string[48];
    public static double[] hourlySolarRadiation = new double[48];
    public static double[] hourlyDirectRadiation = new double[48];
    public static double[] hourlyDiffuseRadiation = new double[48];

    public static DateTime lastUpdate = DateTime.MinValue;
    public static string lastError = "";
    public static string lastApiUrl = "";

    static WeatherDataCache()
    {
        InitializeArrays();
    }

    public static void InitializeArrays()
    {
        for (int i = 0; i < 7; i++)
        {
            forecastTempMax[i] = 0.0;
            forecastTempMin[i] = 0.0;
            forecastApparentTempMax[i] = 0.0;
            forecastApparentTempMin[i] = 0.0;
            forecastConditions[i] = "Unknown";
            forecastWeatherCode[i] = 0.0;
            forecastWindSpeedMax[i] = 0.0;
            forecastUvIndexMax[i] = 0.0;
            forecastSunrise[i] = "";
            forecastSunset[i] = "";
        }

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
}

internal class Measure
{
    private double latitude, longitude;
    private string dataType;
    private int forecastDay;
    private int hourOffset;
    private int updateInterval;
    private API api;
    private string units;
    private string timezone;

    private static DateTime lastGlobalApiCall = DateTime.MinValue;
    private static readonly object apiCallLock = new object();
    private static volatile bool isUpdating = false;
    private const int MIN_API_CALL_INTERVAL = 5;
    private static int activeMeasures = 0;

    internal Measure()
    {
        latitude = 0.0;
        longitude = 0.0;
        dataType = "CurrentTemp";
        forecastDay = 0;
        hourOffset = 0;
        updateInterval = 600;
        units = "metric";
        timezone = "auto";

        lock (apiCallLock)
        {
            activeMeasures++;
        }
    }

    ~Measure()
    {
        lock (apiCallLock)
        {
            activeMeasures--;
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
            WeatherDataCache.lastError = "Invalid coordinates";
            api.Log(API.LogType.Error, $"WeatherX: Invalid coordinates Lat={latitude}, Lon={longitude}");
        }
        else
        {
            WeatherDataCache.lastError = "";
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

        api.Log(API.LogType.Debug, $"WeatherX: Initialized with Lat={latitude}, Lon={longitude}, DataType={dataType}, Units={units}, HourOffset={hourOffset}");

        // Always force update on reload/refresh
        lock (apiCallLock)
        {
            // Reset the last update time to trigger immediate update
            WeatherDataCache.lastUpdate = DateTime.MinValue;
        }
        api.Log(API.LogType.Debug, "WeatherX: Reload detected - forcing data refresh");
    }

    internal double Update()
    {
        // Check if an update is needed based on updateInterval
        double timeSinceLastUpdate = DateTime.Now.Subtract(WeatherDataCache.lastUpdate).TotalSeconds;
        bool needsUpdate = (WeatherDataCache.lastUpdate == DateTime.MinValue) || (timeSinceLastUpdate >= updateInterval);

        if (needsUpdate && !isUpdating)
        {
            lock (apiCallLock)
            {
                // Double-check inside lock to prevent race conditions
                timeSinceLastUpdate = DateTime.Now.Subtract(WeatherDataCache.lastUpdate).TotalSeconds;
                needsUpdate = (WeatherDataCache.lastUpdate == DateTime.MinValue) || (timeSinceLastUpdate >= updateInterval);

                if (needsUpdate && !isUpdating)
                {
                    double timeSinceLastGlobalCall = DateTime.Now.Subtract(lastGlobalApiCall).TotalSeconds;

                    // Ensure minimum interval between API calls to prevent rate limiting
                    if (timeSinceLastGlobalCall >= MIN_API_CALL_INTERVAL)
                    {
                        isUpdating = true;
                        lastGlobalApiCall = DateTime.Now;
                        api?.Log(API.LogType.Debug,
                            $"WeatherX: Triggering update - Time since last: {timeSinceLastUpdate:F1}s, Interval: {updateInterval}s");
                        Task.Run(async () => await UpdateWeatherDataAsync());
                    }
                    else
                    {
                        double waitTime = MIN_API_CALL_INTERVAL - timeSinceLastGlobalCall;
                        api?.Log(API.LogType.Debug,
                            $"WeatherX: Rate limit protection - wait {waitTime:F1}s more (last call: {timeSinceLastGlobalCall:F1}s ago, min: {MIN_API_CALL_INTERVAL}s)");
                    }
                }
            }
        }
        else if (!needsUpdate && WeatherDataCache.lastUpdate != DateTime.MinValue)
        {
            // Log occasionally to show interval is working (every 60 seconds)
            if (DateTime.Now.Second == 0)
            {
                double timeRemaining = updateInterval - timeSinceLastUpdate;
                api?.Log(API.LogType.Debug,
                    $"WeatherX: Next update in {timeRemaining:F0}s (Interval: {updateInterval}s, Last update: {timeSinceLastUpdate:F0}s ago)");
            }
        }

        return GetNumericValue();
    }

    private async Task UpdateWeatherDataAsync()
    {
        try
        {
            WeatherDataCache.lastApiUrl = BuildApiUrl();
            api?.Log(API.LogType.Debug, $"WeatherX: Making API call to: {WeatherDataCache.lastApiUrl}");

            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 |
                System.Net.SecurityProtocolType.Tls11 |
                System.Net.SecurityProtocolType.Tls;

            using (TimeoutWebClient client = new TimeoutWebClient())
            {
                client.Headers.Add("User-Agent", "WeatherX-Rainmeter-Plugin/1.0");
                client.Headers.Add("Accept", "application/json");
                client.Encoding = Encoding.UTF8;
                client.Timeout = 15000;

                string response = await client.DownloadStringTaskAsync(WeatherDataCache.lastApiUrl);
                ParseWeatherData(response);

                WeatherDataCache.lastError = "";
                WeatherDataCache.lastUpdate = DateTime.Now;

                api?.Log(API.LogType.Debug,
                    $"WeatherX: ✓ Data updated successfully at {WeatherDataCache.lastUpdate:HH:mm:ss} | Next update in {updateInterval}s");
            }
        }
        catch (WebException wex)
        {
            if (wex.Response is HttpWebResponse response && response.StatusCode == (HttpStatusCode)429)
            {
                WeatherDataCache.lastError = "Rate limit exceeded - please wait";
                api?.Log(API.LogType.Warning, $"WeatherX: API rate limit hit (429). Waiting before next attempt.");
                WeatherDataCache.lastUpdate = DateTime.Now;
            }
            else
            {
                WeatherDataCache.lastError = $"Error: {wex.Message}";
                api?.Log(API.LogType.Error, $"WeatherX: {WeatherDataCache.lastError}");
            }
        }
        catch (Exception ex)
        {
            WeatherDataCache.lastError = $"Error: {ex.Message}";
            api?.Log(API.LogType.Error, $"WeatherX: {WeatherDataCache.lastError}");
        }
        finally
        {
            isUpdating = false;
        }
    }

    private string BuildApiUrl()
    {
        string tempUnit = units == "imperial" ? "fahrenheit" : "celsius";
        string windUnit = units == "imperial" ? "mph" : "kmh";

        return $"https://api.open-meteo.com/v1/forecast?" +
               $"latitude={latitude.ToString(CultureInfo.InvariantCulture)}&" +
               $"longitude={longitude.ToString(CultureInfo.InvariantCulture)}&" +
               $"current=temperature_2m,relative_humidity_2m,weather_code,surface_pressure,wind_speed_10m," +
               $"apparent_temperature,dew_point_2m,wind_direction_10m,wind_gusts_10m,is_day&" +
               $"hourly=temperature_2m,relative_humidity_2m,wind_speed_10m," +
               $"apparent_temperature,cloud_cover,visibility,weather_code," +
               $"shortwave_radiation,direct_radiation,diffuse_radiation&" +
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
            UpdateCurrentFromHourly();

            api?.Log(API.LogType.Debug, $"WeatherX: Parsing complete - Temp: {WeatherDataCache.currentTemp}°, Condition: {WeatherDataCache.currentCondition}");
        }
        catch (Exception ex)
        {
            api?.Log(API.LogType.Error, $"WeatherX: JSON parsing error: {ex.Message}");
            throw;
        }
    }

    private void ParseCurrentWeather(string json)
    {
        WeatherDataCache.currentTemp = ParseHelper.ParseJsonValue(json, "\"current\"", "\"temperature_2m\"");
        WeatherDataCache.currentHumidity = ParseHelper.ParseJsonValue(json, "\"current\"", "\"relative_humidity_2m\"");
        WeatherDataCache.currentWindSpeed = ParseHelper.ParseJsonValue(json, "\"current\"", "\"wind_speed_10m\"");
        WeatherDataCache.currentPressure = ParseHelper.ParseJsonValue(json, "\"current\"", "\"surface_pressure\"");
        WeatherDataCache.currentApparentTemp = ParseHelper.ParseJsonValue(json, "\"current\"", "\"apparent_temperature\"");
        WeatherDataCache.currentDewPoint = ParseHelper.ParseJsonValue(json, "\"current\"", "\"dew_point_2m\"");
        WeatherDataCache.currentWindDirection = ParseHelper.ParseJsonValue(json, "\"current\"", "\"wind_direction_10m\"");
        WeatherDataCache.currentWindGusts = ParseHelper.ParseJsonValue(json, "\"current\"", "\"wind_gusts_10m\"");
        WeatherDataCache.currentWeatherCode = ParseHelper.ParseJsonValue(json, "\"current\"", "\"weather_code\"");
        WeatherDataCache.currentIsDay = ParseHelper.ParseJsonValue(json, "\"current\"", "\"is_day\"");

        int weatherCode = (int)WeatherDataCache.currentWeatherCode;
        WeatherDataCache.currentCondition = CommonHelper.GetWeatherDescription(weatherCode);

        api?.Log(API.LogType.Debug, $"WeatherX: Current parsed - Temp={WeatherDataCache.currentTemp}, Humidity={WeatherDataCache.currentHumidity}");
    }

    private void UpdateCurrentFromHourly()
    {
        int currentHourIndex = DateTime.Now.Hour;
        if (currentHourIndex < 48)
        {
            WeatherDataCache.currentCloudCover = WeatherDataCache.hourlyCloudCover[currentHourIndex];
            WeatherDataCache.currentSolarRadiation = WeatherDataCache.hourlySolarRadiation[currentHourIndex];
            WeatherDataCache.currentDirectRadiation = WeatherDataCache.hourlyDirectRadiation[currentHourIndex];
            WeatherDataCache.currentDiffuseRadiation = WeatherDataCache.hourlyDiffuseRadiation[currentHourIndex];
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

            ParseHelper.ParseArrayValuesInSection(dailySection, "\"temperature_2m_max\"", WeatherDataCache.forecastTempMax);
            ParseHelper.ParseArrayValuesInSection(dailySection, "\"temperature_2m_min\"", WeatherDataCache.forecastTempMin);
            ParseHelper.ParseArrayValuesInSection(dailySection, "\"apparent_temperature_max\"", WeatherDataCache.forecastApparentTempMax);
            ParseHelper.ParseArrayValuesInSection(dailySection, "\"apparent_temperature_min\"", WeatherDataCache.forecastApparentTempMin);
            ParseHelper.ParseArrayValuesInSection(dailySection, "\"wind_speed_10m_max\"", WeatherDataCache.forecastWindSpeedMax);
            ParseHelper.ParseArrayValuesInSection(dailySection, "\"uv_index_max\"", WeatherDataCache.forecastUvIndexMax);

            ParseHelper.ParseStringArrayInSection(dailySection, "\"sunrise\"", WeatherDataCache.forecastSunrise);
            ParseHelper.ParseStringArrayInSection(dailySection, "\"sunset\"", WeatherDataCache.forecastSunset);

            ParseHelper.ParseArrayValuesInSection(dailySection, "\"weather_code\"", WeatherDataCache.forecastWeatherCode);

            for (int i = 0; i < 7; i++)
            {
                WeatherDataCache.forecastConditions[i] = CommonHelper.GetWeatherDescription((int)WeatherDataCache.forecastWeatherCode[i]);
            }

            if (WeatherDataCache.forecastUvIndexMax.Length > 0)
            {
                WeatherDataCache.todayUvIndex = WeatherDataCache.forecastUvIndexMax[0];
            }

            api?.Log(API.LogType.Debug, $"WeatherX: Daily forecasts parsed - Day 0: Max={WeatherDataCache.forecastTempMax[0]}°");
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

            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"temperature_2m\"", WeatherDataCache.hourlyTemp, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"relative_humidity_2m\"", WeatherDataCache.hourlyHumidity, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"wind_speed_10m\"", WeatherDataCache.hourlyWindSpeed, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"apparent_temperature\"", WeatherDataCache.hourlyApparentTemp, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"cloud_cover\"", WeatherDataCache.hourlyCloudCover, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"visibility\"", WeatherDataCache.hourlyVisibility, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"shortwave_radiation\"", WeatherDataCache.hourlySolarRadiation, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"direct_radiation\"", WeatherDataCache.hourlyDirectRadiation, 48);
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"diffuse_radiation\"", WeatherDataCache.hourlyDiffuseRadiation, 48);

            double[] weatherCodes = new double[48];
            ParseHelper.ParseArrayValuesInSection(hourlySection, "\"weather_code\"", weatherCodes, 48);
            for (int i = 0; i < 48; i++)
            {
                WeatherDataCache.hourlyWeatherCode[i] = (int)weatherCodes[i];
            }

            ParseHelper.ParseTimeArray(hourlySection, WeatherDataCache.hourlyTime);

            api?.Log(API.LogType.Debug, $"WeatherX: Hourly data parsed successfully");
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
            case "currenttemp":
                return WeatherDataCache.currentTemp;
            case "currenthumidity":
                return WeatherDataCache.currentHumidity;
            case "currentwindspeed":
                return WeatherDataCache.currentWindSpeed;
            case "currentpressure":
                return WeatherDataCache.currentPressure;
            case "currentapparenttemp":
                return WeatherDataCache.currentApparentTemp;
            case "currentdewpoint":
                return WeatherDataCache.currentDewPoint;
            case "currentcloudcover":
                return WeatherDataCache.currentCloudCover;
            case "currentwinddirection":
                return WeatherDataCache.currentWindDirection;
            case "currentwindgusts":
                return WeatherDataCache.currentWindGusts;
            case "currentuvindex":
                return WeatherDataCache.todayUvIndex;
            case "currentisday":
                return WeatherDataCache.currentIsDay;
            case "currentweathercode":
                return WeatherDataCache.currentWeatherCode;

            case "currentsolarradiation":
                return WeatherDataCache.currentSolarRadiation;
            case "currentdirectradiation":
                return WeatherDataCache.currentDirectRadiation;
            case "currentdiffuseradiation":
                return WeatherDataCache.currentDiffuseRadiation;

            case "forecasttemp":
            case "forecasttempmax":
                return forecastDay < WeatherDataCache.forecastTempMax.Length ? WeatherDataCache.forecastTempMax[forecastDay] : 0.0;
            case "forecasttempmin":
                return forecastDay < WeatherDataCache.forecastTempMin.Length ? WeatherDataCache.forecastTempMin[forecastDay] : 0.0;
            case "forecastapparenttempmax":
                return forecastDay < WeatherDataCache.forecastApparentTempMax.Length ? WeatherDataCache.forecastApparentTempMax[forecastDay] : 0.0;
            case "forecastapparenttempmin":
                return forecastDay < WeatherDataCache.forecastApparentTempMin.Length ? WeatherDataCache.forecastApparentTempMin[forecastDay] : 0.0;
            case "forecastwindspeed":
                return forecastDay < WeatherDataCache.forecastWindSpeedMax.Length ? WeatherDataCache.forecastWindSpeedMax[forecastDay] : 0.0;
            case "forecastuvindex":
                return forecastDay < WeatherDataCache.forecastUvIndexMax.Length ? WeatherDataCache.forecastUvIndexMax[forecastDay] : 0.0;
            case "forecastweathercode":
                return forecastDay < WeatherDataCache.forecastWeatherCode.Length ? WeatherDataCache.forecastWeatherCode[forecastDay] : 0.0;
            case "forecastsunrise":
                return forecastDay < WeatherDataCache.forecastSunrise.Length ? CommonHelper.ConvertIso8601ToHour(WeatherDataCache.forecastSunrise[forecastDay]) : 0.0;
            case "forecastsunset":
                return forecastDay < WeatherDataCache.forecastSunset.Length ? CommonHelper.ConvertIso8601ToHour(WeatherDataCache.forecastSunset[forecastDay]) : 0.0;

            case "hourlytemp":
                return targetHour < WeatherDataCache.hourlyTemp.Length ? WeatherDataCache.hourlyTemp[targetHour] : 0.0;
            case "hourlyhumidity":
                return targetHour < WeatherDataCache.hourlyHumidity.Length ? WeatherDataCache.hourlyHumidity[targetHour] : 0.0;
            case "hourlywindspeed":
                return targetHour < WeatherDataCache.hourlyWindSpeed.Length ? WeatherDataCache.hourlyWindSpeed[targetHour] : 0.0;
            case "hourlyapparenttemp":
                return targetHour < WeatherDataCache.hourlyApparentTemp.Length ? WeatherDataCache.hourlyApparentTemp[targetHour] : 0.0;
            case "hourlycloudcover":
                return targetHour < WeatherDataCache.hourlyCloudCover.Length ? WeatherDataCache.hourlyCloudCover[targetHour] : 0.0;
            case "hourlyvisibility":
                return targetHour < WeatherDataCache.hourlyVisibility.Length ? WeatherDataCache.hourlyVisibility[targetHour] : 0.0;
            case "hourlyweathercode":
                return targetHour < WeatherDataCache.hourlyWeatherCode.Length ? WeatherDataCache.hourlyWeatherCode[targetHour] : 0.0;

            case "hourlysolarradiation":
                return targetHour < WeatherDataCache.hourlySolarRadiation.Length ? WeatherDataCache.hourlySolarRadiation[targetHour] : 0.0;
            case "hourlydirectradiation":
                return targetHour < WeatherDataCache.hourlyDirectRadiation.Length ? WeatherDataCache.hourlyDirectRadiation[targetHour] : 0.0;
            case "hourlydiffuseradiation":
                return targetHour < WeatherDataCache.hourlyDiffuseRadiation.Length ? WeatherDataCache.hourlyDiffuseRadiation[targetHour] : 0.0;

            case "currenthourtemp":
                return WeatherDataCache.hourlyTemp[DateTime.Now.Hour % 48];
            case "currenthourhumidity":
                return WeatherDataCache.hourlyHumidity[DateTime.Now.Hour % 48];
            case "currenthourwindspeed":
                return WeatherDataCache.hourlyWindSpeed[DateTime.Now.Hour % 48];

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
                return WeatherDataCache.currentCondition;
            case "currentweathercodetext":
                return WeatherDataCache.currentWeatherCode.ToString("F0");
            case "currentisdaytext":
                return WeatherDataCache.currentIsDay > 0 ? "Day" : "Night";
            case "forecastcondition":
                return forecastDay < WeatherDataCache.forecastConditions.Length ? WeatherDataCache.forecastConditions[forecastDay] : "Unknown";
            case "forecastweathercodetext":
                return forecastDay < WeatherDataCache.forecastWeatherCode.Length ? WeatherDataCache.forecastWeatherCode[forecastDay].ToString("F0") : "0";
            case "hourlycondition":
                return targetHour < WeatherDataCache.hourlyWeatherCode.Length ? CommonHelper.GetWeatherDescription(WeatherDataCache.hourlyWeatherCode[targetHour]) : "Unknown";
            case "hourlyweathercodetext":
                return targetHour < WeatherDataCache.hourlyWeatherCode.Length ? WeatherDataCache.hourlyWeatherCode[targetHour].ToString("F0") : "0";
            case "currentwinddirectiontext":
                return CommonHelper.GetWindDirectionText(WeatherDataCache.currentWindDirection);
            case "debugerror":
                return string.IsNullOrEmpty(WeatherDataCache.lastError) ? "No Error" : WeatherDataCache.lastError;
            case "debugurl":
                return WeatherDataCache.lastApiUrl;
            case "debugdaily":
                return $"Day{forecastDay}: Max={WeatherDataCache.forecastTempMax[forecastDay]:F1}°, Min={WeatherDataCache.forecastTempMin[forecastDay]:F1}°, {WeatherDataCache.forecastConditions[forecastDay]}";
            case "debughourly":
                return $"Hour+{hourOffset}: Temp={WeatherDataCache.hourlyTemp[targetHour]:F1}°, {CommonHelper.GetWeatherDescription(WeatherDataCache.hourlyWeatherCode[targetHour])}";
            case "debuglastupdate":
                return WeatherDataCache.lastUpdate == DateTime.MinValue ? "Never" : WeatherDataCache.lastUpdate.ToString("HH:mm:ss");
            case "debugnextupdate":
                if (WeatherDataCache.lastUpdate == DateTime.MinValue)
                    return "On next cycle";
                double timeRemaining = updateInterval - DateTime.Now.Subtract(WeatherDataCache.lastUpdate).TotalSeconds;
                return timeRemaining > 0 ? $"In {timeRemaining:F0}s" : "Now";
            case "debugupdateinterval":
                return $"{updateInterval}s";
            case "status":
                return isUpdating ? "Updating..." : (WeatherDataCache.lastError.Length > 0 ? "Error" : "Ready");
            case "forecastsunrisetext":
                return forecastDay < WeatherDataCache.forecastSunrise.Length ? CommonHelper.ConvertIso8601ToTime(WeatherDataCache.forecastSunrise[forecastDay]) : "N/A";
            case "forecastsunsettext":
                return forecastDay < WeatherDataCache.forecastSunset.Length ? CommonHelper.ConvertIso8601ToTime(WeatherDataCache.forecastSunset[forecastDay]) : "N/A";
            case "hourlytime":
                return targetHour < WeatherDataCache.hourlyTime.Length ? WeatherDataCache.hourlyTime[targetHour] : "";
            case "currenthourlytime":
                {
                    int currentHourIndex = DateTime.Now.Hour % 48;
                    return currentHourIndex < WeatherDataCache.hourlyTime.Length ? WeatherDataCache.hourlyTime[currentHourIndex] : "";
                }
            case "uvindextext":
                return CommonHelper.GetUvIndexDescription(WeatherDataCache.todayUvIndex);
            case "nexthourssummary":
                return GetNextHoursSummary();

            case "debugsolarradiation":
                return $"Solar: {WeatherDataCache.currentSolarRadiation:F1} W/m² | Direct: {WeatherDataCache.currentDirectRadiation:F1} | Diffuse: {WeatherDataCache.currentDiffuseRadiation:F1}";
            case "debugcloudcover":
                return $"Current Clouds: {WeatherDataCache.currentCloudCover:F0}% | Next hour: {(targetHour < WeatherDataCache.hourlyCloudCover.Length ? WeatherDataCache.hourlyCloudCover[targetHour].ToString("F0") + "%" : "N/A")}";

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
            if (hourIndex < WeatherDataCache.hourlyTemp.Length)
            {
                string time = hourIndex < WeatherDataCache.hourlyTime.Length ?
                    CommonHelper.ConvertIso8601ToTime(WeatherDataCache.hourlyTime[hourIndex]) :
                    DateTime.Now.AddHours(i).ToString("HH:mm");

                summary.Append($"{time}: {WeatherDataCache.hourlyTemp[hourIndex]:F0}°");

                if (hourIndex < WeatherDataCache.hourlyWeatherCode.Length)
                {
                    string condition = CommonHelper.GetWeatherDescription(WeatherDataCache.hourlyWeatherCode[hourIndex]);
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
}
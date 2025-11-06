# WeatherX Plugin for Rainmeter

[![License: MIT](https://img.shields.io/badge/License-MIT-lightgrey.svg)]()
[![Version](https://img.shields.io/badge/version-1.1.0-blue.svg)]()
[![Platform](https://img.shields.io/badge/platform-Windows-lightblue.svg)]()

A powerful Rainmeter plugin that provides real-time weather data, 7-day forecasts, hourly predictions, and location information using the free Open-Meteo API.

## ✨ Features

- 🌡️ **Current Weather** - Temperature, humidity, wind, pressure, UV index
- 📅 **7-Day Forecast** - Daily high/low temperatures and conditions
- ⏰ **48-Hour Forecast** - Detailed hourly predictions
- 🌍 **Reverse Geocoding** - Automatic city/state/country detection
- ☀️ **Solar Data** - Radiation, sunrise/sunset times
- 🌙 **Day/Night Detection** - Perfect for dynamic themes
- 📊 **Multiple Units** - Metric or Imperial
- 🚀 **No API Key Required** - Free and unlimited

## 📦 Installation

### Quick Install (Recommended)
1. Download `WeatherX_v*.rmskin` from [Releases](https://github.com/NSTechBytes/WeatherX/releases)
2. Double-click to install
3. Update your coordinates in the skin
4. Done! 🎉

### Manual Install
1. Download the plugin ZIP from [Releases](https://github.com/NSTechBytes/WeatherX/releases)
2. Copy `WeatherX.dll` to `%AppData%\Rainmeter\Plugins\`
   - Use `x64` folder for 64-bit or `x32` for 32-bit
3. Refresh Rainmeter

## 🚀 Quick Start Guide

### Step 1: Basic Setup

Create a parent measure that handles all weather data:

```ini
[Rainmeter]
Update=1000

[Variables]
Latitude=40.7128          ; New York coordinates
Longitude=-74.0060
Units=metric              ; "metric" or "imperial"
UpdateInterval=600        ; Update every 10 minutes

[mWeatherParent]
Measure=Plugin
Plugin=WeatherX.dll
Latitude=#Latitude#
Longitude=#Longitude#
Units=#Units#
UpdateInterval=#UpdateInterval#
DataType=CurrentTemp
```

### Step 2: Add Child Measures

Use child measures to get specific weather data:

```ini
[mTemperature]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=CurrentTemp

[mCondition]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=CurrentCondition

[mHumidity]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=CurrentHumidity
```

### Step 3: Display the Data

```ini
[MeterWeather]
Meter=String
MeasureName=mTemperature
MeasureName2=mCondition
MeasureName3=mHumidity
X=10
Y=10
FontSize=12
FontColor=255,255,255
Text="Temp: %1°#CRLF#Condition: %2#CRLF#Humidity: %3%"
```

### How to Get Your Coordinates

1. Go to [Google Maps](https://maps.google.com)
2. Right-click your location
3. Click the coordinates to copy them
4. Paste into your `Latitude` and `Longitude` variables

## 📊 Available Data Types

### Current Weather

| DataType | Description | Example Value |
|----------|-------------|---------------|
| `CurrentTemp` | Current temperature | 22.5 |
| `CurrentCondition` | Weather description | "Clear Sky" |
| `CurrentHumidity` | Relative humidity | 65 |
| `CurrentWindSpeed` | Wind speed | 15.2 |
| `CurrentWindDirection` | Wind direction degrees | 180 |
| `CurrentWindDirectionText` | Wind direction | "S" |
| `CurrentPressure` | Atmospheric pressure | 1013.2 |
| `CurrentApparentTemp` | Feels-like temperature | 24.1 |
| `CurrentDewPoint` | Dew point | 16.3 |
| `CurrentCloudCover` | Cloud coverage % | 25 |
| `CurrentWindGusts` | Wind gusts speed | 22.5 |
| `CurrentUvIndex` | UV index | 5.2 |
| `CurrentIsDay` | Day=1, Night=0 | 1.0 |
| `CurrentIsDayText` | Day/Night as text | "Day" |
| `CurrentWeatherCode` | Weather code number | 0 |
| `CurrentWeatherCodeText` | Weather code as text | "0" |
| `CurrentSolarRadiation` | Solar radiation W/m² | 450.5 |

### Daily Forecast (0-6 days)

Add `ForecastDay=0` (today) to `ForecastDay=6` (6 days ahead)

| DataType | Description |
|----------|-------------|
| `ForecastTempMax` | Daily maximum temperature |
| `ForecastTempMin` | Daily minimum temperature |
| `ForecastCondition` | Daily weather condition |
| `ForecastApparentTempMax` | Max feels-like temperature |
| `ForecastApparentTempMin` | Min feels-like temperature |
| `ForecastWindSpeed` | Max wind speed |
| `ForecastUvIndex` | Max UV index |
| `ForecastSunrise` | Sunrise hour (numeric) |
| `ForecastSunset` | Sunset hour (numeric) |
| `ForecastSunriseText` | Sunrise time "HH:mm" |
| `ForecastSunsetText` | Sunset time "HH:mm" |
| `ForecastWeatherCode` | Weather code number |
| `ForecastWeatherCodeText` | Weather code as text |

**Example:**
```ini
[mTomorrowMax]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=ForecastTempMax
ForecastDay=1
```

### Hourly Forecast (0-47 hours)

Add `HourOffset=0` (current hour) to `HourOffset=47` (47 hours ahead)

| DataType | Description |
|----------|-------------|
| `HourlyTemp` | Hourly temperature |
| `HourlyCondition` | Hourly weather condition |
| `HourlyHumidity` | Hourly humidity |
| `HourlyWindSpeed` | Hourly wind speed |
| `HourlyApparentTemp` | Hourly feels-like temp |
| `HourlyCloudCover` | Hourly cloud coverage |
| `HourlyVisibility` | Hourly visibility |
| `HourlyWeatherCode` | Weather code number |
| `HourlyWeatherCodeText` | Weather code as text |
| `HourlyTime` | Hour timestamp |
| `HourlySolarRadiation` | Solar radiation W/m² |

**Example:**
```ini
[mNext3Hours]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=HourlyTemp
HourOffset=3
```

### Location Data (Reverse Geocoding)

Enable with `EnableReverseGeocode=1` in parent measure:

| DataType | Description | Example |
|----------|-------------|----------|
| `LocationCity` | City name | "New York" |
| `LocationState` | State/Province | "New York" |
| `LocationStateCode` | State code | "NY" |
| `LocationCountry` | Country name | "United States" |
| `LocationCountryCode` | Country code | "US" |
| `LocationContinent` | Continent name | "North America" |
| `LocationContinentCode` | Continent code | "NA" |
| `LocationFull` | Full location string | "New York, NY, United States" |

**Example:**
```ini
[mWeatherParent]
Measure=Plugin
Plugin=WeatherX.dll
Latitude=#Latitude#
Longitude=#Longitude#
EnableReverseGeocode=1
DataType=CurrentTemp

[mCity]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=LocationCity
```

### Special Data Types

| DataType | Description |
|----------|-------------|
| `UvIndexText` | UV description: "Low", "Moderate", "High", etc. |
| `NextHoursSummary` | Summary of next 6 hours |
| `Status` | Plugin status: "Ready", "Updating...", "Error" |
| `DebugError` | Last error message |
| `DebugUrl` | API URL being used |
| `DebugLastUpdate` | Last update time |
| `DebugNextUpdate` | Time until next update |


## 🌦️ Weather Codes Reference

| Code | Description |
|------|-------------|
| 0 | Clear Sky |
| 1 | Mainly Clear |
| 2 | Partly Cloudy |
| 3 | Overcast |
| 45 | Fog |
| 48 | Depositing Rime Fog |
| 51 | Light Drizzle |
| 53 | Moderate Drizzle |
| 55 | Dense Drizzle |
| 61 | Slight Rain |
| 63 | Moderate Rain |
| 65 | Heavy Rain |
| 71 | Slight Snow |
| 73 | Moderate Snow |
| 75 | Heavy Snow |
| 80 | Slight Rain Showers |
| 81 | Moderate Rain Showers |
| 82 | Violent Rain Showers |
| 95 | Thunderstorm |
| 96 | Thunderstorm with Hail |

## 🚀 Advanced Examples

### Complete Weather Widget

```ini
[Rainmeter]
Update=1000

[Variables]
Latitude=40.7128
Longitude=-74.0060
Units=imperial
UpdateInterval=600

; Parent measure
[mWeatherParent]
Measure=Plugin
Plugin=WeatherX.dll
Latitude=#Latitude#
Longitude=#Longitude#
Units=#Units#
UpdateInterval=#UpdateInterval#
EnableReverseGeocode=1
DataType=CurrentTemp

; Current weather
[mTemp]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=CurrentTemp

[mCondition]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=CurrentCondition

[mHumidity]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=CurrentHumidity

[mWindSpeed]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=CurrentWindSpeed

[mWindDir]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=CurrentWindDirectionText

; Location
[mCity]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=LocationCity

; Display meters
[MeterLocation]
Meter=String
MeasureName=mCity
X=10
Y=10
FontSize=14
FontWeight=700
FontColor=255,255,255
Text="%1"

[MeterTemp]
Meter=String
MeasureName=mTemp
X=10
Y=35
FontSize=48
FontColor=255,255,255
Text="%1°"

[MeterCondition]
Meter=String
MeasureName=mCondition
X=10
Y=90
FontSize=16
FontColor=200,200,200
Text="%1"

[MeterDetails]
Meter=String
MeasureName=mHumidity
MeasureName2=mWindSpeed
MeasureName3=mWindDir
X=10
Y=115
FontSize=12
FontColor=180,180,180
Text="Humidity: %1% | Wind: %2 %3"
```

### 7-Day Forecast

```ini
; Tomorrow's forecast
[mTomorrowHigh]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=ForecastTempMax
ForecastDay=1

[mTomorrowLow]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=ForecastTempMin
ForecastDay=1

[mTomorrowCondition]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=ForecastCondition
ForecastDay=1

[MeterTomorrow]
Meter=String
MeasureName=mTomorrowHigh
MeasureName2=mTomorrowLow
MeasureName3=mTomorrowCondition
X=10
Y=150
FontSize=11
Text="Tomorrow: %1°/%2° - %3"
```

### Dynamic Day/Night Backgrounds

```ini
[mIsDay]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=CurrentIsDay

[mWeatherCode]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=CurrentWeatherCode

[MeterBG]
Meter=Image
; Use different images based on time and weather
; Image naming: day_0.jpg (day, clear), night_61.jpg (night, rain)
ImageName=#@#Backgrounds\[mIsDay]_[mWeatherCode].jpg
W=400
H=300
```

## 🔧 Troubleshooting

### Plugin Not Loading
- Ensure you're using the correct DLL version (x64 vs x32)
- Check Rainmeter log for error messages
- Verify .NET Framework 4.8 is installed

### No Data Showing
- Check your coordinates are correct (use Google Maps)
- Verify internet connection
- Check `DebugError` DataType for error messages
- Ensure `UpdateInterval` is not too short (minimum 60 seconds recommended)

### Location Not Updating
- Make sure `EnableReverseGeocode=1` is set in parent measure
- Location updates when coordinates change
- Check Rainmeter log for geocoding errors

### Debug Example

```ini
[mDebugStatus]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=Status

[mDebugError]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent
DataType=DebugError

[MeterDebug]
Meter=String
MeasureName=mDebugStatus
MeasureName2=mDebugError
Text="Status: %1#CRLF#Error: %2"
```
## 📝 License

MIT License - Free to use and modify

## 👏 Credits

- Weather data: [Open-Meteo API](https://open-meteo.com/)
- Reverse geocoding: [BigDataCloud API](https://www.bigdatacloud.com/)
- Plugin framework: [Rainmeter](https://www.rainmeter.net/)

## 🐛 Issues & Support

Found a bug or have a feature request? [Open an issue](https://github.com/NSTechBytes/WeatherX/issues)

---

Made with ❤️ for the Rainmeter community

# WeatherX Plugin for Rainmeter
[![License: MIT](https://img.shields.io/badge/License-MIT-lightgrey.svg)]()
[![Version](https://img.shields.io/badge/version-1.0-blue.svg)]()
[![Platform](https://img.shields.io/badge/platform-Windows-lightblue.svg)]()

A comprehensive weather plugin for Rainmeter that provides current weather conditions, forecasts, and detailed meteorological data using the Open-Meteo API.

## Preview

![WeatherX Skin Preview](https://github.com/NSTechBytes/WeatherX/blob/main/.github/preview.png)

## Features

- **Real-time Weather Data**: Current temperature, humidity, wind conditions, and more
- **7-Day Forecasts**: Daily temperature ranges and weather conditions
- **48-Hour Forecasts**: Detailed hourly weather predictions
- **Solar Radiation Data**: UV index, solar radiation, direct and diffuse radiation
- **Comprehensive Metrics**: Apparent temperature, dew point, visibility, cloud cover
- **Multiple Units**: Support for metric and imperial units
- **Automatic Updates**: Configurable update intervals
- **Error Handling**: Built-in debugging and status reporting

## Installation

### Option 1: RMSKIN Package (Recommended)
1. Download the latest `WeatherX_v*.rmskin` file from [Releases](https://github.com/NSTechBytes/WeatherX/releases)
2. Double-click the `.rmskin` file to automatically install in Rainmeter
3. Load the WeatherX skin from Rainmeter's skin browser
4. Configure your coordinates in the skin variables

### Option 2: Manual Installation
1. Download the plugin ZIP file from [Releases](https://github.com/NSTechBytes/WeatherX/releases)
2. Extract the appropriate DLL file (`x64` or `x32`) to your Rainmeter plugins folder
3. Place the skin files in your Rainmeter skins directory
4. Configure your coordinates and refresh Rainmeter

## Configuration

### Required Variables

```ini
[Variables]
Latitude=29.696489    ; Your location's latitude
Longitude=72.549843   ; Your location's longitude
Units=metric          ; "metric" or "imperial"
UpdateInterval=300    ; Update frequency in seconds
```

## Measure Options

### DataType Options

The plugin supports numerous `DataType` options for different weather measurements:

#### Current Weather Data

| DataType | Description | Return Type | Units (Metric/Imperial) |
|----------|-------------|-------------|-------------------------|
| `CurrentTemp` | Current temperature | Number | °C / °F |
| `CurrentCondition` | Current weather description | String | Text description |
| `CurrentHumidity` | Relative humidity | Number | % |
| `CurrentWindSpeed` | Wind speed | Number | km/h / mph |
| `CurrentWindDirection` | Wind direction in degrees | Number | 0-360° |
| `CurrentWindDirectionText` | Wind direction as text | String | N, NE, E, etc. |
| `CurrentPressure` | Atmospheric pressure | Number | hPa |
| `CurrentApparentTemp` | Feels-like temperature | Number | °C / °F |
| `CurrentDewPoint` | Dew point temperature | Number | °C / °F |
| `CurrentCloudCover` | Cloud coverage percentage | Number | % |
| `CurrentWindGusts` | Wind gust speed | Number | km/h / mph |
| `CurrentUvIndex` | UV index value | Number | 0-11+ |
| `CurrentSolarRadiation` | Solar radiation | Number | W/m² |
| `CurrentDirectRadiation` | Direct solar radiation | Number | W/m² |
| `CurrentDiffuseRadiation` | Diffuse solar radiation | Number | W/m² |

#### Daily Forecast Data

| DataType | Description | Return Type | Additional Parameter |
|----------|-------------|-------------|----------------------|
| `ForecastTempMax` | Daily maximum temperature | Number | `ForecastDay=0-6` |
| `ForecastTempMin` | Daily minimum temperature | Number | `ForecastDay=0-6` |
| `ForecastCondition` | Daily weather condition | String | `ForecastDay=0-6` |
| `ForecastApparentTempMax` | Daily max apparent temperature | Number | `ForecastDay=0-6` |
| `ForecastApparentTempMin` | Daily min apparent temperature | Number | `ForecastDay=0-6` |
| `ForecastWindSpeed` | Daily max wind speed | Number | `ForecastDay=0-6` |
| `ForecastUvIndex` | Daily max UV index | Number | `ForecastDay=0-6` |
| `ForecastSunrise` | Sunrise time (hour value) | Number | `ForecastDay=0-6` |
| `ForecastSunset` | Sunset time (hour value) | Number | `ForecastDay=0-6` |
| `ForecastSunriseText` | Sunrise time as text | String | `ForecastDay=0-6` |
| `ForecastSunsetText` | Sunset time as text | String | `ForecastDay=0-6` |

#### Hourly Forecast Data

| DataType | Description | Return Type | Additional Parameter |
|----------|-------------|-------------|----------------------|
| `HourlyTemp` | Hourly temperature | Number | `HourOffset=0-47` |
| `HourlyCondition` | Hourly weather condition | String | `HourOffset=0-47` |
| `HourlyHumidity` | Hourly humidity | Number | `HourOffset=0-47` |
| `HourlyWindSpeed` | Hourly wind speed | Number | `HourOffset=0-47` |
| `HourlyApparentTemp` | Hourly apparent temperature | Number | `HourOffset=0-47` |
| `HourlyCloudCover` | Hourly cloud coverage | Number | `HourOffset=0-47` |
| `HourlyVisibility` | Hourly visibility | Number | `HourOffset=0-47` |
| `HourlySolarRadiation` | Hourly solar radiation | Number | `HourOffset=0-47` |
| `HourlyDirectRadiation` | Hourly direct radiation | Number | `HourOffset=0-47` |
| `HourlyDiffuseRadiation` | Hourly diffuse radiation | Number | `HourOffset=0-47` |
| `HourlyTime` | Hourly timestamp | String | `HourOffset=0-47` |

#### Special Data Types

| DataType | Description | Return Type | Purpose |
|----------|-------------|-------------|---------|
| `UvIndexText` | UV index description | String | "Low", "Moderate", "High", etc. |
| `NextHoursSummary` | Summary of next 6 hours | String | Quick forecast overview |
| `Status` | Plugin status | String | "Ready", "Updating...", "Error" |
| `DebugError` | Last error message | String | Troubleshooting |
| `DebugUrl` | API URL being used | String | Debugging |
| `DebugSolarRadiation` | Solar radiation debug info | String | Detailed solar data |
| `DebugCloudCover` | Cloud cover debug info | String | Cloud coverage details |

## Usage Examples

### Basic Temperature Display

```ini
[MeasureCurrentTemp]
Measure=Plugin
Plugin=WeatherX.dll
DataType=CurrentTemp
Latitude=#Latitude#
Longitude=#Longitude#
Units=#Units#
UpdateInterval=#UpdateInterval#

[MeterTemp]
Meter=String
MeasureName=MeasureCurrentTemp
Text=%1°
FontSize=24
```

### Daily Forecast

```ini
[MeasureTomorrowMax]
Measure=Plugin
Plugin=WeatherX.dll
DataType=ForecastTempMax
ForecastDay=1
Latitude=#Latitude#
Longitude=#Longitude#
Units=#Units#
UpdateInterval=#UpdateInterval#

[MeasureTomorrowMin]
Measure=Plugin
Plugin=WeatherX.dll
DataType=ForecastTempMin
ForecastDay=1
Latitude=#Latitude#
Longitude=#Longitude#
Units=#Units#
UpdateInterval=#UpdateInterval#
```

### Hourly Forecast

```ini
[MeasureNext3Hours]
Measure=Plugin
Plugin=WeatherX.dll
DataType=HourlyTemp
HourOffset=3
Latitude=#Latitude#
Longitude=#Longitude#
Units=#Units#
UpdateInterval=#UpdateInterval#
```

### Wind Information

```ini
[MeasureWindSpeed]
Measure=Plugin
Plugin=WeatherX.dll
DataType=CurrentWindSpeed
Latitude=#Latitude#
Longitude=#Longitude#
Units=#Units#
UpdateInterval=#UpdateInterval#

[MeasureWindDirection]
Measure=Plugin
Plugin=WeatherX.dll
DataType=CurrentWindDirectionText
Latitude=#Latitude#
Longitude=#Longitude#
Units=#Units#
UpdateInterval=#UpdateInterval#

[MeterWind]
Meter=String
MeasureName=MeasureWindSpeed
MeasureName2=MeasureWindDirection
Text=Wind: %1 %2
```

## Parameters

### Required Parameters

- **`Latitude`**: Your location's latitude (-90 to 90)
- **`Longitude`**: Your location's longitude (-180 to 180)
- **`DataType`**: The type of weather data to retrieve (see table above)

### Optional Parameters

- **`Units`**: Unit system ("metric" or "imperial", default: "metric")
- **`UpdateInterval`**: Update frequency in seconds (default: 600)
- **`ForecastDay`**: Day offset for forecast data (0-6, default: 0)
- **`HourOffset`**: Hour offset for hourly data (0-47, default: 0)
- **`Timezone`**: Timezone identifier (default: "auto")

## Data Source

This plugin uses the [Open-Meteo API](https://open-meteo.com/), which provides:
- Free weather data
- No API key required
- Global coverage
- High accuracy
- Multiple forecast models

## Error Handling

The plugin includes comprehensive error handling:
- Network timeout protection
- HTTPS/HTTP fallback
- Invalid coordinate detection
- JSON parsing error recovery
- Status reporting via `Status` DataType

## Debugging

Use these DataTypes for troubleshooting:
- `Status`: Current plugin status
- `DebugError`: Last error message
- `DebugUrl`: API URL being called
- `DebugSolarRadiation`: Solar data details
- `DebugCloudCover`: Cloud cover information

## Units

### Metric System (default)
- Temperature: Celsius (°C)
- Wind Speed: km/h
- Pressure: hPa
- Visibility: km
- Radiation: W/m²

### Imperial System
- Temperature: Fahrenheit (°F)
- Wind Speed: mph
- Pressure: hPa (unchanged)
- Visibility: km (unchanged)
- Radiation: W/m² (unchanged)

## Weather Codes

The plugin translates numeric weather codes into descriptive text:

| Code | Description                   |
| ---- | ----------------------------- |
| 0    | Clear Sky                     |
| 1    | Mainly Clear                  |
| 2    | Partly Cloudy                 |
| 3    | Overcast                      |
| 45   | Fog                           |
| 48   | Depositing Rime Fog           |
| 51   | Light Drizzle                 |
| 53   | Moderate Drizzle              |
| 55   | Dense Drizzle                 |
| 56   | Light Freezing Drizzle        |
| 57   | Dense Freezing Drizzle        |
| 61   | Slight Rain                   |
| 63   | Moderate Rain                 |
| 65   | Heavy Rain                    |
| 66   | Light Freezing Rain           |
| 67   | Heavy Freezing Rain           |
| 71   | Slight Snow                   |
| 73   | Moderate Snow                 |
| 75   | Heavy Snow                    |
| 77   | Snow Grains                   |
| 80   | Slight Rain Showers           |
| 81   | Moderate Rain Showers         |
| 82   | Violent Rain Showers          |
| 85   | Slight Snow Showers           |
| 86   | Heavy Snow Showers            |
| 95   | Thunderstorm                  |
| 96   | Thunderstorm with Slight Hail |
| 99   | Thunderstorm with Heavy Hail  |

## Performance

- Automatic HTTP fallback for network issues
- Efficient JSON parsing without external dependencies
- Configurable update intervals to minimize API calls
- Memory-efficient data storage
- Non-blocking asynchronous updates

## Building from Source

### Prerequisites
- Visual Studio 2022 with C++ development tools
- .NET Framework 4.7.2 or later
- PowerShell (for build script)

### Build Instructions
1. Clone the repository
2. Open PowerShell in the project directory
3. Run the build script:
   ```powershell
   powershell -ExecutionPolicy Bypass -Command ". .\Build.ps1; Dist -major 1 -minor 0 -patch 0"
   ```
4. Find the built files in the `dist/` folder:
   - `WeatherX_v*.rmskin` - Complete skin package
   - `WeatherX_v*_x64_x86_dll.zip` - Plugin DLLs only

## Requirements

- Rainmeter 4.5 or later
- .NET Framework 4.7.2 or later
- Internet connection
  
## License

MIT

## Contributing

Contributions are welcome! Please feel free to submit issues and enhancement requests.

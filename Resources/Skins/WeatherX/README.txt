================================================================================
  WEATHERX PLUGIN FOR RAINMETER - USER GUIDE
================================================================================

VERSION: 1.1.0
AUTHOR: NSTechBytes
LICENSE: MIT
GITHUB: https://github.com/NSTechBytes/WeatherX

================================================================================
  GETTING STARTED
================================================================================

1. FIND YOUR COORDINATES
   ------------------------
   - Go to https://maps.google.com
   - Right-click your location
   - Click the coordinates to copy
   - Example: 40.7128, -74.0060
     (First = Latitude, Second = Longitude)

2. EDIT THE SKIN
   --------------
   - Open any .ini file in this folder
   - Find the [Variables] section
   - Update Latitude and Longitude
   - Choose Units (metric or imperial)
   - Save and refresh the skin

3. EXAMPLE SKINS INCLUDED
   -----------------------
   Simple.ini          - Minimal weather display (RECOMMENDED FOR BEGINNERS)
   Main.ini            - Full-featured weather widget with location
   ReverseGeocode.ini  - Demonstrates location detection
   FullExample.ini     - Shows all available features

================================================================================
  BASIC STRUCTURE
================================================================================

PARENT MEASURE (Required - One per location)
---------------------------------------------
Makes the API call and fetches weather data:

[mWeatherParent]
Measure=Plugin
Plugin=WeatherX.dll
Latitude=40.7128
Longitude=-74.0060
Units=metric
UpdateInterval=600
DataType=CurrentTemp

CHILD MEASURES (Get specific data)
-----------------------------------
Don't make API calls - retrieve data from parent:

[mTemp]
Measure=Plugin
Plugin=WeatherX.dll
ParentName=mWeatherParent    ; Links to parent
DataType=CurrentTemp         ; What data to get

DISPLAY METER
-------------
[MeterTemp]
Meter=String
MeasureName=mTemp
Text="%1°"

================================================================================
  POPULAR DATA TYPES
================================================================================

CURRENT WEATHER
---------------
CurrentTemp              - Current temperature
CurrentCondition         - Weather description
CurrentHumidity          - Humidity percentage
CurrentWindSpeed         - Wind speed
CurrentWindDirectionText - Wind direction (N, NE, E, etc.)
CurrentApparentTemp      - Feels-like temperature
CurrentUvIndex           - UV index
CurrentIsDay             - 1.0 = day, 0.0 = night

DAILY FORECAST (Add ForecastDay=0 to 6)
----------------------------------------
ForecastTempMax          - High temperature
ForecastTempMin          - Low temperature
ForecastCondition        - Weather condition
ForecastSunriseText      - Sunrise time
ForecastSunsetText       - Sunset time

Example:
DataType=ForecastTempMax
ForecastDay=1            ; Tomorrow

HOURLY FORECAST (Add HourOffset=0 to 47)
-----------------------------------------
HourlyTemp               - Temperature
HourlyCondition          - Weather condition
HourlyHumidity           - Humidity

Example:
DataType=HourlyTemp
HourOffset=3             ; 3 hours from now

LOCATION (Add EnableReverseGeocode=1 to parent)
------------------------------------------------
LocationCity             - City name
LocationState            - State/Province
LocationCountry          - Country name
LocationFull             - Full location string

================================================================================
  UNITS
================================================================================

METRIC (default)
----------------
Temperature: Celsius (°C)
Wind Speed: km/h
Pressure: hPa

IMPERIAL
--------
Temperature: Fahrenheit (°F)
Wind Speed: mph
Pressure: hPa

Set in parent measure:
Units=metric     or     Units=imperial

================================================================================
  TROUBLESHOOTING
================================================================================

NO DATA SHOWING?
----------------
✓ Check coordinates are correct
✓ Verify internet connection
✓ Check Rainmeter log (Ctrl+G)
✓ Use DataType=DebugError to see errors

PLUGIN NOT LOADING?
-------------------
✓ Using correct DLL version (x64 vs x32)?
✓ .NET Framework 4.8 installed?
✓ Try restarting Rainmeter

LOCATION NOT SHOWING?
---------------------
✓ Add EnableReverseGeocode=1 to parent measure
✓ Wait for first update (default 10 minutes)

================================================================================
  TIPS & BEST PRACTICES
================================================================================

1. Use ONE parent measure per location
   Create child measures for each data point

2. Update Interval
   Don't set below 60 seconds (respect the free API)
   Default 600 seconds (10 minutes) is recommended

3. Testing
   Use DataType=Status to check if plugin is working
   Use DebugError to see any error messages

4. Multiple Locations
   Create separate parent measures with different names

5. Dynamic Themes
   Use CurrentIsDay to change colors/images for day/night
   Use CurrentWeatherCode for weather-specific themes

================================================================================
  WEATHER CODES
================================================================================

0  = Clear Sky              61 = Slight Rain
1  = Mainly Clear           63 = Moderate Rain
2  = Partly Cloudy          65 = Heavy Rain
3  = Overcast               71 = Slight Snow
45 = Fog                    73 = Moderate Snow
51 = Light Drizzle          75 = Heavy Snow
53 = Moderate Drizzle       80 = Rain Showers
55 = Dense Drizzle          95 = Thunderstorm

================================================================================
  COMPLETE DOCUMENTATION
================================================================================

For complete documentation, examples, and troubleshooting:
- README.md - Full documentation
- QUICKSTART.md - Step-by-step beginner guide
- GitHub: https://github.com/NSTechBytes/WeatherX

================================================================================
  SUPPORT
================================================================================

Found a bug? Have a question?
Open an issue: https://github.com/NSTechBytes/WeatherX/issues

Made with ❤️ for the Rainmeter community
================================================================================

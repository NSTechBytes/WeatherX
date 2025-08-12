using System.Globalization;
using System;
using Rainmeter;

public static class Parser
{
    public static string ExtractHourlySection(string json, int hourlyStart)
    {
        int braceStart = json.IndexOf("{", hourlyStart);
        if (braceStart == -1) return "";

        int braceCount = 0;
        int currentPos = braceStart;

        do
        {
            if (json[currentPos] == '{') braceCount++;
            if (json[currentPos] == '}') braceCount--;
            currentPos++;
        }
        while (braceCount > 0 && currentPos < json.Length);

        if (braceCount == 0)
        {
            return json.Substring(braceStart, currentPos - braceStart);
        }

        return "";
    }

    public static string ExtractDailySection(string json, int dailyStart)
    {
        int braceStart = json.IndexOf("{", dailyStart);
        if (braceStart == -1) return "";

        int braceCount = 0;
        int currentPos = braceStart;

        do
        {
            if (json[currentPos] == '{') braceCount++;
            if (json[currentPos] == '}') braceCount--;
            currentPos++;
        }
        while (braceCount > 0 && currentPos < json.Length);

        if (braceCount == 0)
        {
            return json.Substring(braceStart, currentPos - braceStart);
        }

        return "";
    }

    public static void ParseArrayValuesInSection(string section, string arrayName, double[] outputArray, int maxItems = -1)
    {
        try
        {
            if (string.IsNullOrEmpty(section)) return;

            int arrayStart = section.IndexOf(arrayName);
            if (arrayStart == -1) return;

            int bracketStart = section.IndexOf("[", arrayStart);
            if (bracketStart == -1) return;

            int bracketEnd = section.IndexOf("]", bracketStart);
            if (bracketEnd == -1) return;

            string arrayContent = section.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
            string[] values = arrayContent.Split(',');

            int itemsToProcess = maxItems > 0 ? Math.Min(maxItems, Math.Min(values.Length, outputArray.Length))
                                              : Math.Min(values.Length, outputArray.Length);

            for (int i = 0; i < itemsToProcess; i++)
            {
                string value = values[i].Trim().Replace("\"", "");

                if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    outputArray[i] = 0.0;
                    continue;
                }

                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                {
                    outputArray[i] = result;
                }
                else
                {
                    outputArray[i] = 0.0;
                }
            }
        }
        catch { }
    }

    public static void ParseTimeArray(string section, string[] outputArray)
    {
        try
        {
            int arrayStart = section.IndexOf("\"time\"");
            if (arrayStart == -1) return;

            int bracketStart = section.IndexOf("[", arrayStart);
            if (bracketStart == -1) return;

            int bracketEnd = section.IndexOf("]", bracketStart);
            if (bracketEnd == -1) return;

            string arrayContent = section.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
            string[] values = arrayContent.Split(',');

            for (int i = 0; i < Math.Min(values.Length, outputArray.Length); i++)
            {
                string value = values[i].Trim().Replace("\"", "");
                outputArray[i] = value;
            }
        }
        catch { }
    }
    public static void ParseStringArrayInSection(string section, string arrayName, string[] outputArray)
    {
        try
        {
            if (string.IsNullOrEmpty(section)) return;

            int arrayStart = section.IndexOf(arrayName);
            if (arrayStart == -1) return;

            int bracketStart = section.IndexOf("[", arrayStart);
            if (bracketStart == -1) return;

            int bracketEnd = section.IndexOf("]", bracketStart);
            if (bracketEnd == -1) return;

            string arrayContent = section.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
            string[] values = arrayContent.Split(',');

            for (int i = 0; i < Math.Min(values.Length, outputArray.Length); i++)
            {
                string value = values[i].Trim().Replace("\"", "");

                if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    outputArray[i] = "";
                    continue;
                }

                outputArray[i] = value;
            }
        }
        catch { }
    }
}

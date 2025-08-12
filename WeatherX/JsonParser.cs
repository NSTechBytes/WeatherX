using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class JsonParser
{
    public static double ParseJsonValue(string json, string section, string key)
    {
        try
        {

            int sectionStart = json.IndexOf(section);
            if (sectionStart == -1) return 0.0;

            int braceStart = json.IndexOf("{", sectionStart);
            if (braceStart == -1) return 0.0;

            int keyStart = json.IndexOf(key, braceStart);
            if (keyStart == -1) return 0.0;

            int colonPos = json.IndexOf(":", keyStart);
            if (colonPos == -1) return 0.0;

            int valueStart = colonPos + 1;
            int valueEnd = json.IndexOfAny(new char[] { ',', '}', ']' }, valueStart);
            if (valueEnd == -1) return 0.0;

            string valueStr = json.Substring(valueStart, valueEnd - valueStart).Trim();

            if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
            {
                valueStr = valueStr.Substring(1, valueStr.Length - 2);
            }

            if (double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return 0.0;
        }
        catch
        {
            return 0.0;
        }
    }
}
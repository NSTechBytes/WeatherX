using Rainmeter;
using System.Runtime.InteropServices;
using System;

public static class Plugin
{
    static IntPtr StringBuffer = IntPtr.Zero;

    [DllExport]
    public static void Initialize(ref IntPtr data, IntPtr rm)
    {
        data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
    }

    [DllExport]
    public static void Finalize(IntPtr data)
    {
        GCHandle.FromIntPtr(data).Free();

        if (StringBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(StringBuffer);
            StringBuffer = IntPtr.Zero;
        }
    }

    [DllExport]
    public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
    {
        Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
        measure.Reload(new Rainmeter.API(rm), ref maxValue);
    }

    [DllExport]
    public static double Update(IntPtr data)
    {
        Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
        return measure.Update();
    }

    [DllExport]
    public static IntPtr GetString(IntPtr data)
    {
        Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;

        if (StringBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(StringBuffer);
            StringBuffer = IntPtr.Zero;
        }

        string value = measure.GetStringValue();
        StringBuffer = Marshal.StringToHGlobalUni(value);
        return StringBuffer;
    }
}
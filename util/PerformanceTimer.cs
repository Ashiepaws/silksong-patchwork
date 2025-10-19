using System.Collections.Generic;

namespace Patchwork.Util;

public static class PerformanceTimer
{
    private static readonly Dictionary<string, System.Diagnostics.Stopwatch> Timers = new();
    public static void Start(string label)
    {
        if (!Plugin.Config.EnablePerformanceTimers) return;

        var timer = new System.Diagnostics.Stopwatch();
        timer.Start();
        Timers[label] = timer;
    }

    public static void Stop(string label)
    {
        if (!Plugin.Config.EnablePerformanceTimers) return;
        
        if (Timers.ContainsKey(label))
        {
            Timers[label].Stop();
            Plugin.Logger.LogInfo($"[PerformanceTimer] {label}: {Timers[label].ElapsedMilliseconds} ms");
            Timers.Remove(label);
        }
        else
        {
            Plugin.Logger.LogWarning($"[PerformanceTimer] No timer found for label: {label}");
        }
    }
}
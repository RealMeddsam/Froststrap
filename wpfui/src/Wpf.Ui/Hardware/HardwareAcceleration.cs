using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Wpf.Ui.Hardware;
/// <summary>
/// Set of tools for hardware acceleration.
/// </summary>
public static class HardwareAcceleration
{
    /// <summary>
    /// Determines whether the provided rendering tier is supported.
    /// </summary>
    /// <param name="tier">Hardware acceleration rendering tier to check.</param>
    /// <returns><see langword="true"/> if tier is supported.</returns>
    public static bool IsSupported(RenderingTier tier)
    {
        return RenderCapability.Tier >> 16 >= (int)tier;
    }

    /// <summary>
    /// Determines if full hardware acceleration (Tier 2) is available.
    /// </summary>
    public static bool IsHardwareAccelerationEnabled =>
        (RenderCapability.Tier >> 16) >= 2;

    /// <summary>
    /// Disables all WPF animations by forcing their duration to zero.
    /// Call once at app startup before animations begin.
    /// </summary>
    public static void DisableAllAnimations()
    {
        // Use 3 FPS as minimal safe frame rate to avoid errors
        Timeline.DesiredFrameRateProperty.OverrideMetadata(
            typeof(Timeline),
            new FrameworkPropertyMetadata(3));
    }

    /// <summary>
    /// Aggressively frees memory by forcing garbage collection and trimming working set.
    /// </summary>
    public static void MemoryTrimming()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        NativeMethods.SetProcessWorkingSetSize(
            Process.GetCurrentProcess().Handle, -1, -1);
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetProcessWorkingSetSize(IntPtr process, int minSize, int maxSize);
    }
}

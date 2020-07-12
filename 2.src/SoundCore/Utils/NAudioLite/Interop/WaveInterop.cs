using NAudio;
using NAudio.Wave;
using System;
using System.Runtime.InteropServices;

namespace SoundCore.Utils.NAudioLite.Interop
{
    /// <summary>
    /// MME Wave function interop
    /// </summary>
    class WaveInterop
    {
        [DllImport("winmm.dll")]
        public static extern Int32 waveOutGetNumDevs();

        // http://msdn.microsoft.com/en-us/library/dd743857%28VS.85%29.aspx
        [DllImport("winmm.dll"
#if !WINDOWS_UWP
        , CharSet = CharSet.Auto
#endif
        )]
        public static extern MmResult waveOutGetDevCaps(IntPtr deviceID, out WaveOutCapabilities waveOutCaps, int waveOutCapsSize);

        [DllImport("winmm.dll")]
        public static extern Int32 waveInGetNumDevs();

        // http://msdn.microsoft.com/en-us/library/dd743841%28VS.85%29.aspx
        [DllImport("winmm.dll"
#if !WINDOWS_UWP
        , CharSet = CharSet.Auto
#endif
        )]
        public static extern MmResult waveInGetDevCaps(IntPtr deviceID, out WaveInCapabilities waveInCaps, int waveInCapsSize);
    }
}

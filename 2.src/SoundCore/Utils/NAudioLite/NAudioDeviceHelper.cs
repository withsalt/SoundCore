using NAudio;
using NAudio.Wave;
using SoundCore.Utils.NAudioLite.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SoundCore.Utils.NAudioLite
{
    public class NAudioDeviceHelper
    {
        public static int DeviceInCount
        {
            get
            {
                return WaveInterop.waveInGetNumDevs();
            }
        }

        public static int DeviceOutCount
        {
            get
            {
                return WaveInterop.waveOutGetNumDevs();
            }
        }

        /// <summary>
        /// Retrieves the capabilities of a waveIn device
        /// </summary>
        /// <param name="devNumber">Device to test</param>
        /// <returns>The WaveIn device capabilities</returns>
        public static WaveInCapabilities GetInCapabilities(int devNumber)
        {
            var caps = new WaveInCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveInGetDevCaps((IntPtr)devNumber, out caps, structSize), "waveInGetDevCaps");
            return caps;
        }

        public static WaveOutCapabilities GetOutCapabilities(int devNumber)
        {
            var caps = new WaveOutCapabilities();
            var structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveOutGetDevCaps((IntPtr)devNumber, out caps, structSize), "waveOutGetDevCaps");
            return caps;
        }
    }
}

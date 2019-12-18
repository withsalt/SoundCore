using System;
using System.Collections.Generic;
using System.Text;

namespace SoundCore
{
    public class SoundConnectionSettings
    {
        /// <summary>
        /// The playback device name of the sound device is connected to.
        /// </summary>
        public string PlaybackDeviceName { get; set; } = "default";

        /// <summary>
        /// The recording device name of the sound device is connected to.
        /// </summary>
        public string RecordingDeviceName { get; set; } = "default";

        /// <summary>
        /// The mixer device name of the sound device is connected to.
        /// </summary>
        public string MixerDeviceName { get; set; } = "default";

        /// <summary>
        /// The sample rate of recording.
        /// </summary>
        public uint RecordingSampleRate { get; set; } = 16000;

        /// <summary>
        /// The channels of recording. 
        /// </summary>
        public ushort RecordingChannels { get; set; } = 1;

        /// <summary>
        /// The bits per sample of recording.
        /// </summary>
        public ushort RecordingBitsPerSample { get; set; } = 16;

        /// <summary>
        /// 
        /// </summary>
        public int MaxBufferLength { get; set; } = 10240000;
    }
}

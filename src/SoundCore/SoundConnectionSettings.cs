using System;
using System.Collections.Generic;
using System.Text;

namespace SoundCore
{
    public class SoundConnectionSettings
    {
        public SoundConnectionSettings()
        {

        }

        public SoundConnectionSettings(uint sampleRate)
        {
            this.SampleRate = sampleRate;
        }

        public SoundConnectionSettings(uint sampleRate, ushort channels)
        {
            this.SampleRate = sampleRate;
            this.Channels = channels;
        }

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
        public uint SampleRate { get; set; } = 16000;

        /// <summary>
        /// The channels of recording. 
        /// </summary>
        public ushort Channels { get; set; } = 1;

        /// <summary>
        /// The bits per sample of recording.
        /// </summary>
        public ushort BitsPerSample { get; set; } = 16;

        /// <summary>
        /// 
        /// </summary>
        public int MaxBufferLength { get; set; } = 10240000;
    }
}

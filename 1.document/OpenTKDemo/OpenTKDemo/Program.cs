using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenTKDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            IList<string> _recorders = AudioCapture.AvailableDevices;
            if (_recorders == null || _recorders.Count == 0)
            {
                Console.WriteLine("No available devices");
                return;
            }
            int sampling_rate = 24000;
            int bufferSize = 4096;
            short[] buffer = new short[bufferSize];
            const byte sampleToByte = 2;

            using (var audioCapture = new AudioCapture(_recorders.First(), sampling_rate, ALFormat.Mono16, bufferSize))
            {
                Console.CancelKeyPress += (sender, e) =>
                {
                    Console.WriteLine("Exit...");
                    audioCapture.Dispose();
                };
                audioCapture.Start();

                while (true)
                {
                    int available_samples = audioCapture.AvailableSamples;

                    if (available_samples * sampleToByte > buffer.Length * BlittableValueType.StrideOf(buffer))
                    {
                        buffer = new short[MathHelper.NextPowerOfTwo(
                            (int)(available_samples * sampleToByte / (double)BlittableValueType.StrideOf(buffer) + 0.5))];
                    }
                    if (available_samples > 0)
                    {
                        audioCapture.ReadSamples(buffer, available_samples);
                        int buf = AL.GenBuffer();
                        AL.BufferData(buf, ALFormat.Mono16, buffer, (int)(available_samples * BlittableValueType.StrideOf(buffer)), audioCapture.SampleFrequency);
                        Console.WriteLine(available_samples+"....");
                    }
                }
            }
        }

        //public MusicPlayer(MusicOptions options)
        //{
        //    if (options == null)
        //        throw new ArgumentNullException("options");

        //    this.bufferSize = options.BufferSize;
        //    this.bufferCount = options.BufferCount;
        //    this.samplingRate = options.SamplingRate;
        //    this.updateInterval = options.UpdateInterval;

        //    this.context = new AudioContext();
        //    this.master = new Master(options.SamplingRate, 23);
        //    this.preset = new Preset();
        //    this.layer = new Dictionary<string, SequenceLayer>();

        //    this.source = AL.GenSource();
        //    this.buffers = AL.GenBuffers(this.bufferCount);
        //    this.sbuf = new short[this.bufferSize];
        //    this.fbuf = new float[this.bufferSize];

        //    foreach (int buffer in this.buffers)
        //        this.FillBuffer(buffer);

        //    AL.SourceQueueBuffers(this.source, this.buffers.Length, this.buffers);
        //    AL.SourcePlay(this.source);

        //    this.Updater = Task.Factory.StartNew(this.Update);
        //}
    }
}

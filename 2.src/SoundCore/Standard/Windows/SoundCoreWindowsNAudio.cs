using NAudio.CoreAudioApi;
using NAudio.Wave;
using SoundCore.Enums;
using SoundCore.Model;
using SoundCore.Utils.NAudioLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SoundCore.Standard
{
    public sealed class SoundCoreWindowsNAudio : ISoundCore
    {
        private static readonly Queue<BufferCache> _cache = new Queue<BufferCache>();
        private static readonly object _cacheLocker = new object();
        private static readonly object _playTaskLocker = new object();
        private static Task _playDataTask = null;
        private static IWaveIn _waveIn = null;
        private CancellationToken ct;

        public SoundConnectionSettings Settings { get; internal set; }

        /// <summary>
        /// 录音结果
        /// </summary>
        public event EventHandler<RecordEventArgs> OnMessage;

        public SoundCoreWindowsNAudio(SoundConnectionSettings settings)
        {
            this.Settings = settings ?? throw new ArgumentNullException(nameof(SoundConnectionSettings));
        }

        public void Dispose()
        {
            try
            {
                StopPlay();
                StopRecord();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Play

        public void Play(byte[] data, CancellationToken ct)
        {
            if (_playDataTask == null || _playDataTask.Status != TaskStatus.Running)
            {
                lock (_playTaskLocker)
                {
                    _playDataTask = Task.Run(() => PlayDataAsync());
                    //Wait for task start.
                    while (_playDataTask.Status != TaskStatus.Running)
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            lock (_cacheLocker)
            {
                if (ct.IsCancellationRequested)
                    _cache.Enqueue(new BufferCache(data, true));
                else
                    _cache.Enqueue(new BufferCache(data, false));
            }
        }

        public void PlayWav(byte[] data)
        {
            using Stream stream = new MemoryStream(data);
            using WaveStream readerStream = new BlockAlignReductionStream(WaveFormatConversionStream.CreatePcmStream(new WaveFileReader(stream)));
            PlayWavAsync(readerStream).GetAwaiter().GetResult();
        }

        public void PlayWav(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception($"File({path}) is not exist.");
            }
             WaveStream readerStream = new WaveFileReader(path);
            //if wav is not pcm
            if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                readerStream = new BlockAlignReductionStream(readerStream);
            }
            PlayWavAsync(readerStream).GetAwaiter().GetResult();
            readerStream.Dispose();
        }

        public async Task PlayWavAsync(byte[] data)
        {

        }

        public async Task PlayWavAsync(string path)
        {

        }


        #region Private

        private async Task PlayWavAsync(WaveStream stream)
        {
            if (stream == null)
            {
                throw new Exception("WaveStream is null");
            }
            using WaveOutEvent waveOut = new WaveOutEvent();
            waveOut.Init(stream);
            waveOut.Play();
            while (waveOut.PlaybackState == PlaybackState.Playing)
            {
                await Task.Delay(10);
            }
        }

        private async void PlayDataAsync()
        {
            bool status = false;
            try
            {
                using WaveOutEvent wo = new WaveOutEvent();
                BufferedWaveProvider rs = new BufferedWaveProvider(new WaveFormat((int)Settings.SampleRate, Settings.Channels))
                {
                    BufferLength = Settings.MaxBufferLength,  //请保证播放缓冲区足够大
                    DiscardOnBufferOverflow = true
                };
                wo.Init(rs);
                wo.Play();

                while (!status)
                {
                    BufferCache data = null;
                    lock (_cacheLocker)
                    {
                        if (_cache.Count == 0)
                            continue;
                        data = _cache.Dequeue();
                    }
                    if (data == null)
                        continue;
                    if (data.Data != null)
                        rs.AddSamples(data.Data, 0, data.Data.Length);
                    if (status = data.IsEnd)
                        break;
                }
                while (wo.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(1);
                    if (rs.BufferedBytes == 0 && status)
                    {
                        await Task.Delay(300);
                        wo.Stop();
                        wo.Dispose();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                lock (_cacheLocker)
                {
                    _cache.Clear();
                }
            }
        }

        #endregion

        #endregion

        #region Record


        public void Record(CancellationToken ct)
        {
            if (ct != null)
            {
                this.ct = ct;
            }
            _waveIn = new WaveInEvent();
            _waveIn.DataAvailable += (s, a) =>
            {
                OnMessage?.Invoke(this, new RecordEventArgs()
                {
                    Buffer = a.Buffer,
                    Length = a.BytesRecorded
                });
            };
            _waveIn.StartRecording();
        }

        public void RecordWav(string path, int second)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                _waveIn = new WaveInEvent();
                WaveFileWriter writer = new WaveFileWriter(outStream, _waveIn.WaveFormat);
                _waveIn.DataAvailable += (s, a) =>
                {
                    writer.Write(a.Buffer, 0, a.BytesRecorded);
                };
                _waveIn.RecordingStopped += (s, e) =>
                {
                    byte[] bytes = new byte[outStream.Length];
                    outStream.Seek(0, SeekOrigin.Begin);
                    outStream.Read(bytes, 0, bytes.Length);


                    OnMessage?.Invoke(this, new RecordEventArgs()
                    {
                        Buffer = bytes,
                        Length = bytes.Length
                    });
                };
                _waveIn.StartRecording();
            }
        }

        public bool StopPlay()
        {
            try
            {
                if (_playDataTask != null && _playDataTask.Status == TaskStatus.Running)
                {
                    lock (_playTaskLocker)
                    {
                        this.Play(null, new CancellationToken(true));
                        while (_playDataTask.Status == TaskStatus.Running)
                        {
                            Thread.Sleep(1);
                        }
                        _playDataTask.Dispose();
                        _playDataTask = null;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public bool StopRecord()
        {
            try
            {
                if (_waveIn != null)
                {
                    _waveIn.StopRecording();
                    _waveIn.Dispose();
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region Private

        public void Pause()
        {
            throw new NotImplementedException();
        }



        #endregion

        #endregion

        #region Other
        public List<SoundDevice> ListDevices()
        {
            List<SoundDevice> devices = new List<SoundDevice>();

            MMDeviceEnumerator enumberator = new MMDeviceEnumerator();
            MMDeviceCollection deviceCollection = enumberator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);

            for (int i = 0; i < NAudioDeviceHelper.DeviceInCount; i++)
            {
                WaveInCapabilities deviceInfo = NAudioDeviceHelper.GetInCapabilities(i);
                if (deviceInfo.ProductGuid == Guid.Empty || string.IsNullOrEmpty(deviceInfo.ProductName))
                {
                    continue;
                }
                var mmDevices = deviceCollection.Where(d => d.FriendlyName.StartsWith(deviceInfo.ProductName)).ToList();
                if (mmDevices != null && mmDevices.Count > 0)
                {
                    foreach (MMDevice device in mmDevices)
                    {
                        devices.Add(new SoundDevice()
                        {
                            Id = deviceInfo.ProductGuid.ToString(),
                            Name = device.FriendlyName,
                            Type = DeviceType.Output
                        });
                    }
                }
            }

            for (int i = 0; i < NAudioDeviceHelper.DeviceOutCount; i++)
            {
                WaveOutCapabilities deviceInfo = NAudioDeviceHelper.GetOutCapabilities(i);
                if (deviceInfo.ProductGuid == Guid.Empty || string.IsNullOrEmpty(deviceInfo.ProductName))
                {
                    continue;
                }
                var mmDevices = deviceCollection.Where(d => d.FriendlyName.StartsWith(deviceInfo.ProductName)).ToList();
                if (mmDevices != null && mmDevices.Count > 0)
                {
                    foreach (MMDevice device in mmDevices)
                    {
                        devices.Add(new SoundDevice()
                        {
                            Id = deviceInfo.ProductGuid.ToString(),
                            Name = device.FriendlyName,
                            Type = DeviceType.Output
                        });
                    }
                }
            }
            return devices;
        }
        #endregion
    }
}

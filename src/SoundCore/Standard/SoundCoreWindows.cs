using NAudio.Wave;
using SoundCore.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SoundCore.Standard
{
    public class SoundCoreWindows : ISoundCore
    {
        private static readonly Queue<DataCache> _cache = new Queue<DataCache>();
        private static readonly object _cacheLocker = new object();
        private static Task _playDataTask = null;
        private static IWaveIn _waveIn = null;

        private static SoundConnectionSettings _settings;

        /// <summary>
        /// 录音结果
        /// </summary>
        public event EventHandler<RecordEventArgs> OnMessage;

        public SoundCoreWindows(SoundConnectionSettings settings)
        {
            if (settings == null)
            {
                throw new Exception("SoundConnectionSettings can not null.");
            }
            _settings = settings;
        }

        public void Dispose()
        {

        }

        public void Play(byte[] data, bool isLast = false)
        {
            
            if (_playDataTask == null || _playDataTask.Status != TaskStatus.Running)
            {
                _playDataTask = Task.Run(() => PlayDataAsync());
                //Wait for task start.
                while(_playDataTask.Status != TaskStatus.Running)
                {
                    Thread.Sleep(1);
                }
            }
            lock (_cacheLocker)
            {
                _cache.Enqueue(new DataCache(data, isLast));
            }
        }

        public async Task PlayWav(byte[] data)
        {
            using (Stream stream = new MemoryStream(data))
            {
                using (WaveStream readerStream = new BlockAlignReductionStream(WaveFormatConversionStream.CreatePcmStream(new WaveFileReader(stream))))
                {
                    await PlayWavAsync(readerStream);
                }
            }
        }

        public async Task PlayWav(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception("Play file is not exist.");
            }
            WaveStream readerStream = new WaveFileReader(path);
            if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                readerStream = new BlockAlignReductionStream(readerStream);
            }
            await PlayWavAsync(readerStream);
            readerStream.Dispose();
        }


        public void Record()
        {
            using (Stream outStream = new MemoryStream(_settings.MaxBufferLength))
            {
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
        }

        public void RecordWav()
        {
            long length = 0;
            using (Stream outStream = new MemoryStream(_settings.MaxBufferLength))
            {
                _waveIn = new WaveInEvent();
                WaveFileWriter writer = new WaveFileWriter(outStream, _waveIn.WaveFormat);
                _waveIn.DataAvailable += (s, a) =>
                {
                    length += a.BytesRecorded;
                    writer.Write(a.Buffer, 0, a.BytesRecorded);
                };
                _waveIn.RecordingStopped += (s, e) =>
                {
                    byte[] bytes = new byte[length];
                    outStream.Read(bytes, 0, bytes.Length);
                    outStream.Seek(0, SeekOrigin.Begin);

                    OnMessage?.Invoke(this, new RecordEventArgs()
                    {
                        Buffer = bytes,
                        Length = length
                    });
                };
                _waveIn.StartRecording();
            }
        }

        public bool Stop()
        {
            if (_waveIn != null)
            {
                _waveIn.StopRecording();
            }
            return true;
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
                await Task.Delay(50);
            }
        }

        private async void PlayDataAsync()
        {
            bool status = false;
            try
            {
                using WaveOutEvent wo = new WaveOutEvent();
                BufferedWaveProvider rs = new BufferedWaveProvider(new WaveFormat((int)_settings.SampleRate, _settings.Channels))
                {
                    BufferLength = _settings.MaxBufferLength,  //针对长文本，请标准缓冲区足够大
                    DiscardOnBufferOverflow = true
                };
                wo.Init(rs);
                wo.Play();

                while (!status)
                {
                    DataCache data = null;
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

        public void Pause()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

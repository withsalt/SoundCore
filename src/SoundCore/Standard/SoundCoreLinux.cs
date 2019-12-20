using SoundCore.Model;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SoundCore.Standard
{
    public class SoundCoreLinux : ISoundCore
    {
        private IntPtr _playbackPcm;
        private IntPtr _recordingPcm;
        private IntPtr _mixer;
        private IntPtr _elem;
        private int _errorNum;

        private static readonly object playbackInitializationLock = new object();
        private static readonly object recordingInitializationLock = new object();
        private static readonly object mixerInitializationLock = new object();

        private static readonly Queue<DataCache> _cache = new Queue<DataCache>();
        private static readonly object _cacheLocker = new object();
        private static Task _playDataTask = null;

        private static SoundConnectionSettings _settings = null;

        public SoundCoreLinux(SoundConnectionSettings settings)
        {
            _settings = settings;
        }

        public event EventHandler<RecordEventArgs> OnMessage;

        #region Play

        public void Play(byte[] data, bool isLast = false)
        {
            if (_playDataTask == null || _playDataTask.Status != TaskStatus.Running)
            {
                _playDataTask = Task.Run(() => PlayDataAsync());
                while (_playDataTask.Status != TaskStatus.Running)
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
            try
            {
                IntPtr @params = new IntPtr();
                int dir = 0;
                WavHeader header;
                byte[] pcm = new byte[data.Length - 44];
                //构建header，并获取wav的音频数据。
                await using (MemoryStream ms = new MemoryStream(data))
                {
                    header = GetWavHeader(ms);
                    ms.Seek(44, SeekOrigin.Begin);
                    ms.Read(pcm, 0, data.Length - 44);
                }

                OpenPlaybackPcm();
                PcmInitialize(_playbackPcm, header, ref @params, ref dir);
                WriteStream(pcm, header, ref @params, ref dir);

                while (true)
                {
                    await Task.Delay(30);
                    Console.WriteLine(@params + "," + dir);
                }

                ClosePlaybackPcm();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new Exception($"Play wav failed. {ex.Message}", ex);
            }
        }

        public async Task PlayWav(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception("Play file is not exist.");
            }

            byte[] read = File.ReadAllBytes(path);
            await PlayWav(read);
        }

        private void PlayDataAsync()
        {
            bool status = false;
            IntPtr @params = new IntPtr();
            int dir = 0;
            try
            {
                OpenPlaybackPcm();
                WavHeader header = CreateWavHeader(_settings);
                PcmInitialize(_playbackPcm, header, ref @params, ref dir);
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
                    {
                        WriteStream(data.Data, header, ref @params, ref dir);
                    }
                    if (status == data.IsEnd)
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                lock (_cacheLocker)
                {
                    _cache.Clear();
                }

                status = false;
                ClosePlaybackPcm();
            }
        }

        #region Private

        private void OpenPlaybackPcm()
        {
            if (_playbackPcm != default)
            {
                return;
            }

            lock (playbackInitializationLock)
            {
                _errorNum = Interop.snd_pcm_open(ref _playbackPcm, _settings.PlaybackDeviceName,
                    snd_pcm_stream_t.SND_PCM_STREAM_PLAYBACK, 0);
                ThrowErrorMessage(_errorNum, "Can not open playback device.");
            }
        }

        private void ClosePlaybackPcm()
        {
            if (_playbackPcm != default)
            {
                _errorNum = Interop.snd_pcm_drop(_playbackPcm);
                ThrowErrorMessage(_errorNum, "Drop playback device error.");

                _errorNum = Interop.snd_pcm_close(_playbackPcm);
                ThrowErrorMessage(_errorNum, "Close playback device error.");

                _playbackPcm = default;
            }
        }

        #endregion

        #endregion

        #region Record

        public void Record()
        {
            throw new NotImplementedException();
        }

        public void RecordWav()
        {
            throw new NotImplementedException();
        }

        public bool Stop()
        {
            throw new NotImplementedException();
        }

        #region Private

        private void OpenRecordingPcm()
        {
            if (_recordingPcm != default)
            {
                return;
            }

            lock (recordingInitializationLock)
            {
                _errorNum = Interop.snd_pcm_open(ref _recordingPcm, _settings.RecordingDeviceName,
                    snd_pcm_stream_t.SND_PCM_STREAM_CAPTURE, 0);
                ThrowErrorMessage(_errorNum, "Can not open recording device.");
            }
        }

        private void CloseRecordingPcm()
        {
            if (_recordingPcm != default)
            {
                _errorNum = Interop.snd_pcm_drop(_recordingPcm);
                ThrowErrorMessage(_errorNum, "Drop recording device error.");

                _errorNum = Interop.snd_pcm_close(_recordingPcm);
                ThrowErrorMessage(_errorNum, "Close recording device error.");

                _recordingPcm = default;
            }
        }

        #endregion

        #endregion

        #region Common

        protected void Dispose()
        {
            ClosePlaybackPcm();
            CloseRecordingPcm();
            CloseMixer();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        #region Private

        private unsafe void PcmInitialize(IntPtr pcm, WavHeader header, ref IntPtr @params, ref int dir)
        {
            _errorNum = Interop.snd_pcm_hw_params_malloc(ref @params);
            ThrowErrorMessage(_errorNum, "Can not allocate parameters object.");

            _errorNum = Interop.snd_pcm_hw_params_any(pcm, @params);
            ThrowErrorMessage(_errorNum, "Can not fill parameters object.");

            _errorNum = Interop.snd_pcm_hw_params_set_access(pcm, @params,
                snd_pcm_access_t.SND_PCM_ACCESS_RW_INTERLEAVED);
            ThrowErrorMessage(_errorNum, "Can not set access mode.");

            _errorNum = (int) (header.BitsPerSample / 8) switch
            {
                1 => Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_U8),
                2 => Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S16_LE),
                3 => Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S24_LE),
                _ => throw new Exception("Bits per sample error. Please reset the value of RecordingBitsPerSample."),
            };
            ThrowErrorMessage(_errorNum, "Can not set format.");

            _errorNum = Interop.snd_pcm_hw_params_set_channels(pcm, @params, header.NumChannels);
            ThrowErrorMessage(_errorNum, "Can not set channel.");

            uint val = header.SampleRate;
            fixed (int* dirP = &dir)
            {
                _errorNum = Interop.snd_pcm_hw_params_set_rate_near(pcm, @params, &val, dirP);
                ThrowErrorMessage(_errorNum, "Can not set rate.");
            }

            _errorNum = Interop.snd_pcm_hw_params(pcm, @params);
            ThrowErrorMessage(_errorNum, "Can not set hardware parameters.");
        }

        private unsafe void WriteStream(byte[] data, WavHeader header, ref IntPtr @params, ref int dir)
        {
            ulong frames;
            fixed (int* dirP = &dir)
            {
                _errorNum = Interop.snd_pcm_hw_params_get_period_size(@params, &frames, dirP);
                ThrowErrorMessage(_errorNum, "Can not get period size.");
            }

            int bufferSize = (int) frames * header.BlockAlign;
            // In Interop, the frames is defined as ulong. But actucally, the value of bufferSize won't be too big.
            byte[] readBuffer = new byte[bufferSize];
            fixed (byte* buffer = readBuffer)
            {
                for (int i = 0; i < data.Length; i += bufferSize)
                {
                    if (data.Length - i < bufferSize)
                    {
                        Array.Copy(data, i, readBuffer, 0, data.Length - i);
                        Array.Clear(readBuffer, data.Length - i, bufferSize - (data.Length - i));
                    }
                    else
                    {
                        Array.Copy(data, i, readBuffer, 0, bufferSize);
                    }

                    _errorNum = Interop.snd_pcm_writei(_playbackPcm, (IntPtr) buffer, frames);
                    ThrowErrorMessage(_errorNum, "Can not write data to the device.");
                }
            }
        }

        private unsafe void ReadStream(Stream saveStream, WavHeader header, ref IntPtr @params, ref int dir)
        {
            ulong frames, bufferSize;

            fixed (int* dirP = &dir)
            {
                _errorNum = Interop.snd_pcm_hw_params_get_period_size(@params, &frames, dirP);
                ThrowErrorMessage(_errorNum, "Can not get period size.");
            }

            bufferSize = frames * header.BlockAlign;
            byte[] readBuffer = new byte[(int) bufferSize];
            saveStream.Position = 44;

            fixed (byte* buffer = readBuffer)
            {
                for (int i = 0; i < (int) (header.Subchunk2Size / bufferSize); i++)
                {
                    _errorNum = Interop.snd_pcm_readi(_recordingPcm, (IntPtr) buffer, frames);
                    ThrowErrorMessage(_errorNum, "Can not read data from the device.");

                    saveStream.Write(readBuffer);
                }
            }

            saveStream.Flush();
        }

        private void OpenMixer()
        {
            if (_mixer != default)
            {
                return;
            }

            lock (mixerInitializationLock)
            {
                _errorNum = Interop.snd_mixer_open(ref _mixer, 0);
                ThrowErrorMessage(_errorNum, "Can not open sound device mixer.");

                _errorNum = Interop.snd_mixer_attach(_mixer, _settings.MixerDeviceName);
                ThrowErrorMessage(_errorNum, "Can not attach sound device mixer.");

                _errorNum = Interop.snd_mixer_selem_register(_mixer, IntPtr.Zero, IntPtr.Zero);
                ThrowErrorMessage(_errorNum, "Can not register sound device mixer.");

                _errorNum = Interop.snd_mixer_load(_mixer);
                ThrowErrorMessage(_errorNum, "Can not load sound device mixer.");

                _elem = Interop.snd_mixer_first_elem(_mixer);
            }
        }

        private void CloseMixer()
        {
            if (_mixer != default)
            {
                _errorNum = Interop.snd_mixer_close(_mixer);
                ThrowErrorMessage(_errorNum, "Close sound device mixer error.");

                _mixer = default;
                _elem = default;
            }
        }

        private WavHeader GetWavHeader(Stream wavStream)
        {
            Span<byte> readBuffer2 = stackalloc byte[2];
            Span<byte> readBuffer4 = stackalloc byte[4];
            wavStream.Position = 0;
            WavHeader header = new WavHeader();
            try
            {
                wavStream.Read(readBuffer4);
                header.ChunkId = Encoding.ASCII.GetString(readBuffer4).ToCharArray();

                wavStream.Read(readBuffer4);
                header.ChunkSize = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

                wavStream.Read(readBuffer4);
                header.Format = Encoding.ASCII.GetString(readBuffer4).ToCharArray();

                wavStream.Read(readBuffer4);
                header.Subchunk1ID = Encoding.ASCII.GetString(readBuffer4).ToCharArray();

                wavStream.Read(readBuffer4);
                header.Subchunk1Size = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

                wavStream.Read(readBuffer2);
                header.AudioFormat = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer2);

                wavStream.Read(readBuffer2);
                header.NumChannels = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer2);

                wavStream.Read(readBuffer4);
                header.SampleRate = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

                wavStream.Read(readBuffer4);
                header.ByteRate = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

                wavStream.Read(readBuffer2);
                header.BlockAlign = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer2);

                wavStream.Read(readBuffer2);
                header.BitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer2);

                wavStream.Read(readBuffer4);
                header.Subchunk2Id = Encoding.ASCII.GetString(readBuffer4).ToCharArray();

                wavStream.Read(readBuffer4);
                header.Subchunk2Size = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);
            }
            catch
            {
                throw new Exception("Non-standard WAV file.");
            }

            return header;
        }

        private WavHeader CreateWavHeader(SoundConnectionSettings settings)
        {
            try
            {
                WavHeader header = new WavHeader
                {
                    ChunkId = new[] {'R', 'I', 'F', 'F'},
                    ChunkSize =
                        0, //second * _settings.SampleRate * _settings.BitsPerSample * _settings.Channels / 8 + 36,
                    Format = new[] {'W', 'A', 'V', 'E'},
                    Subchunk1ID = new[] {'f', 'm', 't', ' '},
                    Subchunk1Size = 16,
                    AudioFormat = 1, //PCM音频数据的值为1
                    NumChannels = _settings.Channels,
                    SampleRate = _settings.SampleRate,
                    ByteRate = _settings.SampleRate * _settings.BitsPerSample * _settings.Channels / 8,
                    BlockAlign = (ushort) (_settings.BitsPerSample * _settings.Channels / 8),
                    BitsPerSample = _settings.BitsPerSample,
                    Subchunk2Id = new[] {'d', 'a', 't', 'a'},
                    Subchunk2Size = 0 //second * _settings.SampleRate * _settings.BitsPerSample * _settings.Channels / 8
                };

                return header;
            }
            catch (Exception ex)
            {
                throw new Exception($"Create wav header failed. {ex.Message}", ex);
            }
        }

        private void ThrowErrorMessage(int errnum, string message)
        {
            if (errnum < 0)
            {
                string errorMsg = Marshal.PtrToStringAnsi(Interop.snd_strerror(errnum));
                Dispose();
                throw new Exception($"{message}\nError {errnum}. {errorMsg}.");
            }
        }

        #endregion

        #endregion
    }
}
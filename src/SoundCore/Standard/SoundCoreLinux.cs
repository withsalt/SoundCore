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
            using (MemoryStream ms = new MemoryStream(data))
            {
                await PlayWavAsync(ms);
            }
        }

        public async Task PlayWav(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception("Play file is not exist.");
            }
            using (FileStream fs = File.Open(path, FileMode.Open))
            {
                await PlayWavAsync(fs);
            }
        }

        /// <summary>
        /// Play WAV file.
        /// </summary>
        /// <param name="wavStream">WAV stream.</param>
        private async Task PlayWavAsync(Stream wavStream)
        {
            try
            {
                IntPtr @params = new IntPtr();
                int dir = 0;
                WavHeader header = GetWavHeader(wavStream);

                OpenPlaybackPcm();
                PcmInitialize(_playbackPcm, header, ref @params, ref dir);
                //skip wav header
                wavStream.Position = 44;
                WriteStream(wavStream, header, ref @params, ref dir);
                ClosePlaybackPcm();
            }
            catch(Exception ex)
            {
                throw new Exception($"Play wav failed. {ex.Message}", ex);
            }
        }

        private async void PlayDataAsync()
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
                        using (MemoryStream ms = new MemoryStream(data.Data))
                        {
                            if(WriteStream(ms, header, ref @params, ref dir))
                            {
                                continue;
                            }
                        }
                    } 
                    if (status = data.IsEnd)
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
                _errorNum = Interop.snd_pcm_open(ref _playbackPcm, _settings.PlaybackDeviceName, snd_pcm_stream_t.SND_PCM_STREAM_PLAYBACK, 0);
                ThrowErrorMessage("Can not open playback device.");
            }
        }

        private void ClosePlaybackPcm()
        {
            if (_playbackPcm != default)
            {
                _errorNum = Interop.snd_pcm_drop(_playbackPcm);
                ThrowErrorMessage("Drop playback device error.");

                _errorNum = Interop.snd_pcm_close(_playbackPcm);
                ThrowErrorMessage("Close playback device error.");

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
                _errorNum = Interop.snd_pcm_open(ref _recordingPcm, _settings.RecordingDeviceName, snd_pcm_stream_t.SND_PCM_STREAM_CAPTURE, 0);
                ThrowErrorMessage("Can not open recording device.");
            }
        }

        private void CloseRecordingPcm()
        {
            if (_recordingPcm != default)
            {
                _errorNum = Interop.snd_pcm_drop(_recordingPcm);
                ThrowErrorMessage("Drop recording device error.");

                _errorNum = Interop.snd_pcm_close(_recordingPcm);
                ThrowErrorMessage("Close recording device error.");

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
            ThrowErrorMessage("Can not allocate parameters object.");

            _errorNum = Interop.snd_pcm_hw_params_any(pcm, @params);
            ThrowErrorMessage("Can not fill parameters object.");

            _errorNum = Interop.snd_pcm_hw_params_set_access(pcm, @params, snd_pcm_access_t.SND_PCM_ACCESS_RW_INTERLEAVED);
            ThrowErrorMessage("Can not set access mode.");

            _errorNum = (int)(header.BitsPerSample / 8) switch
            {
                1 => Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_U8),
                2 => Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S16_LE),
                3 => Interop.snd_pcm_hw_params_set_format(pcm, @params, snd_pcm_format_t.SND_PCM_FORMAT_S24_LE),
                _ => throw new Exception("Bits per sample error. Please reset the value of RecordingBitsPerSample."),
            };
            ThrowErrorMessage("Can not set format.");

            _errorNum = Interop.snd_pcm_hw_params_set_channels(pcm, @params, header.NumChannels);
            ThrowErrorMessage("Can not set channel.");

            uint val = header.SampleRate;
            fixed (int* dirP = &dir)
            {
                _errorNum = Interop.snd_pcm_hw_params_set_rate_near(pcm, @params, &val, dirP);
                ThrowErrorMessage("Can not set rate.");
            }

            _errorNum = Interop.snd_pcm_hw_params(pcm, @params);
            ThrowErrorMessage("Can not set hardware parameters.");
        }

        private unsafe bool WriteStream(Stream wavStream, WavHeader header, ref IntPtr @params, ref int dir)
        {
            ulong frames, bufferSize;
            fixed (int* dirP = &dir)
            {
                _errorNum = Interop.snd_pcm_hw_params_get_period_size(@params, &frames, dirP);
                ThrowErrorMessage("Can not get period size.");
            }
            bufferSize = frames * header.BlockAlign;
            // In Interop, the frames is defined as ulong. But actucally, the value of bufferSize won't be too big.
            byte[] readBuffer = new byte[(int)bufferSize];

            fixed (byte* buffer = readBuffer)
            {
                while (wavStream.Read(readBuffer) != 0)
                {
                    _errorNum = Interop.snd_pcm_writei(_playbackPcm, (IntPtr)buffer, frames);
                    ThrowErrorMessage("Can not write data to the device.");
                }
            }
            return true;
        }

        private unsafe void ReadStream(Stream saveStream, WavHeader header, ref IntPtr @params, ref int dir)
        {
            ulong frames, bufferSize;

            fixed (int* dirP = &dir)
            {
                _errorNum = Interop.snd_pcm_hw_params_get_period_size(@params, &frames, dirP);
                ThrowErrorMessage("Can not get period size.");
            }

            bufferSize = frames * header.BlockAlign;
            byte[] readBuffer = new byte[(int)bufferSize];
            saveStream.Position = 44;

            fixed (byte* buffer = readBuffer)
            {
                for (int i = 0; i < (int)(header.Subchunk2Size / bufferSize); i++)
                {
                    _errorNum = Interop.snd_pcm_readi(_recordingPcm, (IntPtr)buffer, frames);
                    ThrowErrorMessage("Can not read data from the device.");

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
                ThrowErrorMessage("Can not open sound device mixer.");

                _errorNum = Interop.snd_mixer_attach(_mixer, _settings.MixerDeviceName);
                ThrowErrorMessage("Can not attach sound device mixer.");

                _errorNum = Interop.snd_mixer_selem_register(_mixer, IntPtr.Zero, IntPtr.Zero);
                ThrowErrorMessage("Can not register sound device mixer.");

                _errorNum = Interop.snd_mixer_load(_mixer);
                ThrowErrorMessage("Can not load sound device mixer.");

                _elem = Interop.snd_mixer_first_elem(_mixer);
            }
        }

        private void CloseMixer()
        {
            if (_mixer != default)
            {
                _errorNum = Interop.snd_mixer_close(_mixer);
                ThrowErrorMessage("Close sound device mixer error.");

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
                WavHeader header = new WavHeader();

                return header;
            }
            catch (Exception ex)
            {
                throw new Exception($"Create wav header failed. {ex.Message}", ex);
            }
        }

        private void ThrowErrorMessage(string message)
        {
            if (_errorNum < 0)
            {
                int code = _errorNum;
                string errorMsg = Marshal.PtrToStringAnsi(Interop.snd_strerror(_errorNum));

                Dispose();
                throw new Exception($"{message}\nError {code}. {errorMsg}.");
            }
        }
        #endregion

        #endregion
    }
}

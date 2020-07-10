using SoundCore.Enums;
using SoundCore.Model;
using SoundIOSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SoundCore.Standard
{
    public class SoundCoreLinuxAlsa : ISoundCore
    {
        public event EventHandler<RecordEventArgs> OnMessage;

        public SoundConnectionSettings Settings { get; internal set; }

        public SoundCoreLinuxAlsa(SoundConnectionSettings settings)
        {
            if (settings == null)
            {
                throw new Exception("SoundConnectionSettings can not null.");
            }
            this.Settings = settings;
        }

        event EventHandler<RecordEventArgs> ISoundCore.OnMessage
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Play(byte[] data, bool isLast = false)
        {
            throw new NotImplementedException();
        }

        public Task PlayWav(byte[] data)
        {
            throw new NotImplementedException();
        }

        public Task PlayWav(string path)
        {
            throw new NotImplementedException();
        }

        public void Record()
        {
            throw new NotImplementedException();
        }

        public void RecordWav(string path, int second)
        {
            throw new NotImplementedException();
        }

        public bool Stop()
        {
            throw new NotImplementedException();
        }

        Task ISoundCore.PlayWav(byte[] data)
        {
            throw new NotImplementedException();
        }

        Task ISoundCore.PlayWav(string path)
        {
            throw new NotImplementedException();
        }

        void ISoundCore.Play(byte[] data, bool isLast)
        {
            throw new NotImplementedException();
        }

        void ISoundCore.RecordWav(string path, int second)
        {
            throw new NotImplementedException();
        }

        void ISoundCore.Record()
        {
            throw new NotImplementedException();
        }

        void ISoundCore.Pause()
        {
            throw new NotImplementedException();
        }

        bool ISoundCore.Stop()
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }

        public List<SoundDevice> ListDevices()
        {
            List<SoundDevice> devices = new List<SoundDevice>();
            using (var api = new SoundIO())
            {
                api.Connect();
                api.FlushEvents();

                for (int i = 0; i < api.InputDeviceCount; i++)
                {
                    SoundIODevice device = api.GetInputDevice(i);
                    if (device == null)
                        continue;
                    
                        devices.Add(new SoundDevice()
                    {
                        Id = device.Id,
                        Name = device.Name,
                        Type = DeviceType.Input,
                    });
                }
                for (int i = 0; i < api.OutputDeviceCount; i++)
                {
                    SoundIODevice device = api.GetOutputDevice(i);
                    if (device == null)
                        continue;
                    
                    devices.Add(new SoundDevice()
                    {
                        Id = device.Id,
                        Name = device.Name,
                        Type = DeviceType.Output,
                    });
                }
            }
            return devices;
        }
    }
}
using SoundCore.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SoundCore.Standard
{
    public class SoundCoreLinux : ISoundCore
    {
        private static readonly Queue<CacheBuffer> _cache = new Queue<CacheBuffer>();
        private static readonly object _cacheLocker = new object();
        private static bool _status = false;

        private static SoundConnectionSettings _settings = null;

        public SoundCoreLinux(SoundConnectionSettings settings)
        {
            _settings = settings;
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

        public void RecordWav()
        {
            throw new NotImplementedException();
        }

        public bool Stop()
        {
            throw new NotImplementedException();
        }
    }
}

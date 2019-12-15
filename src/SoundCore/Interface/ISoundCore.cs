using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SoundCore
{
    public interface ISoundCore
    {
        Task PlayWav(byte[] data);

        Task PlayWav(string path);

        void Play(byte[] data, bool isLast = false);

        void RecordWav();

        void Record();

        bool Stop();
    }
}

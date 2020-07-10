using System;
using System.Collections.Generic;
using System.Text;

namespace SoundCore.Model
{
    public class BufferCache
    {
        public BufferCache()
        {

        }

        public BufferCache(byte[] buffer)
        {
            this.Data = buffer;
            this.IsEnd = false;
        }

        public BufferCache(byte[] buffer, bool isEnd)
        {
            this.Data = buffer;
            this.IsEnd = isEnd;
        }

        public byte[] Data { get; set; }

        public bool IsEnd { get; set; } = false;
    }
}

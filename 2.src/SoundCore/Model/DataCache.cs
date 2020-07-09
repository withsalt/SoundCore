using System;
using System.Collections.Generic;
using System.Text;

namespace SoundCore.Model
{
    public class DataCache
    {
        public DataCache()
        {

        }

        public DataCache(byte[] buffer)
        {
            this.Data = buffer;
            this.IsEnd = false;
        }

        public DataCache(byte[] buffer, bool isEnd)
        {
            this.Data = buffer;
            this.IsEnd = isEnd;
        }

        public byte[] Data { get; set; }

        public bool IsEnd { get; set; } = false;
    }
}

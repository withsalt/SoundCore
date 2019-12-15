using System;
using System.Collections.Generic;
using System.Text;

namespace SoundCore.Model
{
    public class RecordEventArgs
    {
        public byte[] Buffer { get; set; }

        public long Length { get; set; }
    }
}

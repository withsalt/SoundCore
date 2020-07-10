using SoundCore.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SoundCore.Model
{
    public class SoundDevice
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public DeviceType Type { get; set; }
    }
}

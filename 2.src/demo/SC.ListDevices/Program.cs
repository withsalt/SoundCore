using SoundCore;
using SoundCore.Model;
using System;
using System.Collections.Generic;

namespace SC.ListDevices
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ISoundCore api = SoundCoreBuilder.Create(new SoundConnectionSettings()))
            {
                List<SoundDevice> device= api.ListDevices();
            }
        }
    }
}

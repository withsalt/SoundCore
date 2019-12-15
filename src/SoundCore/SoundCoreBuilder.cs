using SoundCore.Standard;
using System;
using System.Runtime.InteropServices;

namespace SoundCore
{
    public class SoundCoreBuilder
    {
        public static ISoundCore Create(SoundConnectionSettings settings)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new SoundCoreLinux(settings);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new SoundCoreWindows(settings);
            }
            else
            {
                throw new Exception("Unsupport system. SoundCore only support Linux and Windows");
            }
        }
    }
}

﻿using SoundCore.Standard;
using SoundIOSharp;
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
                //Test libasound2-dev
                try
                {
                    _ = SoundIO.VersionString;
                }
                catch
                {
                    throw new Exception("Plase install 'libasound2-dev' at first.");
                }
                return new SoundCoreLinuxAlsa(settings);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new SoundCoreWindowsNAudio(settings);
            }
            else
            {
                throw new Exception("Unsupport system. SoundCore only support Linux and Windows");
            }
        }
    }
}

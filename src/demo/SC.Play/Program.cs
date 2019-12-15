using SoundCore;
using System;
using System.IO;

namespace SC.Play
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] data = File.ReadAllBytes("audio/01.pcm");

            ISoundCore api = SoundCoreBuilder.Create(new SoundConnectionSettings()
            {

            });

            //api.PlayWav(data);

            api.Play(data);


            Console.ReadKey(false);
        }
    }
}

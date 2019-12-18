using SoundCore;
using System;
using System.IO;
using System.Threading;

namespace SC.Play
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] data = File.ReadAllBytes("audio/04.wav");
            int frameSize = 3200;

            ISoundCore api = SoundCoreBuilder.Create(new SoundConnectionSettings());

            //for (int i = 0; i < data.Length; i += frameSize)
            //{
            //    api.Play(SubArray(data, i, frameSize));
            //}
            //api.Play(null, true);

            api.PlayWav(data);

            Console.WriteLine("播放结束。");
            Console.ReadKey(false);
        }

        private static byte[] SubArray(byte[] source, int startIndex, int length)
        {
            if (startIndex < 0 || startIndex > source.Length || length < 0)
            {
                return null;
            }
            byte[] Destination;
            if (startIndex + length <= source.Length)
            {
                Destination = new byte[length];
                Array.Copy(source, startIndex, Destination, 0, length);
            }
            else
            {
                Destination = new byte[(source.Length - startIndex)];
                Array.Copy(source, startIndex, Destination, 0, source.Length - startIndex);
            }
            return Destination;
        }
    }
}

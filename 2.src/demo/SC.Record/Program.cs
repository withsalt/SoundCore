using SoundCore;
using System;

namespace SC.Record
{
    class Program
    {
        static void Main(string[] args)
        {
            ISoundCore api = SoundCoreBuilder.Create(new SoundConnectionSettings());

            //api.PlayWav(data);
            api.OnMessage += (sender, e) =>
            {
                Console.WriteLine($"Length:{e.Length}");
            };

            api.Record();

            Console.WriteLine("录音中....\n请按任意键暂停录音。");
            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(false);
                if (keyInfo.Key == ConsoleKey.Q)
                {
                    api.Stop();
                    break;
                }
            }

            Console.ReadKey(false);
        }

        private static void Api_OnMessage(object sender, RecordEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}

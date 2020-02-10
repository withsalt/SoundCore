using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SoundCore
{
    public interface ISoundCore : IDisposable
    {
        #region Event

        /// <summary>
        /// 数据事件
        /// 参数RecordEventArgs
        /// </summary>
        event EventHandler<RecordEventArgs> OnMessage;

        #endregion

        #region Method

        /// <summary>
        /// 播放Wav数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task PlayWav(byte[] data);

        /// <summary>
        /// 播放Wav文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task PlayWav(string path);

        /// <summary>
        /// 播放数据流
        /// </summary>
        /// <param name="data"></param>
        /// <param name="isLast"></param>
        void Play(byte[] data, bool isLast = false);

        /// <summary>
        /// 录音为Wav
        /// </summary>
        void RecordWav();

        /// <summary>
        /// 录音为PCM
        /// </summary>
        void Record();

        /// <summary>
        /// 暂停
        /// </summary>
        void Pause();

        /// <summary>
        /// 停止
        /// </summary>
        /// <returns></returns>
        bool Stop();

        #endregion
    }
}
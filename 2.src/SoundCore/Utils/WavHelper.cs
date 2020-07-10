using SoundCore.Model;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SoundCore.Utils
{
    public class WavHelper
    {
        internal WavHeader GetWavHeader(Stream wavStream)
        {
            Span<byte> readBuffer2 = stackalloc byte[2];
            Span<byte> readBuffer4 = stackalloc byte[4];
            wavStream.Position = 0;
            WavHeader header = new WavHeader();
            try
            {
                wavStream.Read(readBuffer4);
                header.ChunkId = Encoding.ASCII.GetString(readBuffer4).ToCharArray();

                wavStream.Read(readBuffer4);
                header.ChunkSize = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

                wavStream.Read(readBuffer4);
                header.Format = Encoding.ASCII.GetString(readBuffer4).ToCharArray();

                wavStream.Read(readBuffer4);
                header.Subchunk1ID = Encoding.ASCII.GetString(readBuffer4).ToCharArray();

                wavStream.Read(readBuffer4);
                header.Subchunk1Size = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

                wavStream.Read(readBuffer2);
                header.AudioFormat = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer2);

                wavStream.Read(readBuffer2);
                header.NumChannels = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer2);

                wavStream.Read(readBuffer4);
                header.SampleRate = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

                wavStream.Read(readBuffer4);
                header.ByteRate = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);

                wavStream.Read(readBuffer2);
                header.BlockAlign = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer2);

                wavStream.Read(readBuffer2);
                header.BitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(readBuffer2);

                wavStream.Read(readBuffer4);
                header.Subchunk2Id = Encoding.ASCII.GetString(readBuffer4).ToCharArray();

                wavStream.Read(readBuffer4);
                header.Subchunk2Size = BinaryPrimitives.ReadUInt32LittleEndian(readBuffer4);
            }
            catch
            {
                throw new Exception("Non-standard WAV file.");
            }

            return header;
        }

        internal WavHeader CreateWavHeader(SoundConnectionSettings settings, int second)
        {
            try
            {
                WavHeader header = new WavHeader
                {
                    ChunkId = new[] { 'R', 'I', 'F', 'F' },
                    ChunkSize = (uint)(second * settings.SampleRate * settings.BitsPerSample * settings.Channels / 8 + 36),
                    Format = new[] { 'W', 'A', 'V', 'E' },
                    Subchunk1ID = new[] { 'f', 'm', 't', ' ' },
                    Subchunk1Size = 16,
                    AudioFormat = 1,
                    NumChannels = settings.Channels,
                    SampleRate = settings.SampleRate,
                    ByteRate = settings.SampleRate * settings.BitsPerSample * settings.Channels / 8,
                    BlockAlign = (ushort)(settings.BitsPerSample * settings.Channels / 8),
                    BitsPerSample = settings.BitsPerSample,
                    Subchunk2Id = new[] { 'd', 'a', 't', 'a' },
                    Subchunk2Size = (uint)(second * settings.SampleRate * settings.BitsPerSample * settings.Channels / 8)
                };
                return header;
            }
            catch (Exception ex)
            {
                throw new Exception($"Create wav header failed. {ex.Message}", ex);
            }
        }
    }
}

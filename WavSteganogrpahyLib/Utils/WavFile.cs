using WavSteganographyLib.Base;

namespace WavSteganographyLib.Utils
{
    public static class WavFile
    {
        public static WavData ReadWavFile(string file)
        {
            using var stream = File.OpenRead(file);
            using var reader = new BinaryReader(stream);

            reader.ReadBytes(4);
            reader.ReadInt32();
            reader.ReadBytes(4);

            ushort audioFormat = 0;
            ushort channels = 0;
            uint sampleRate = 0;
            uint byteRate = 0;
            ushort blockAlign = 0;
            ushort bitsPerSample = 0;
            var samples = new List<short>();

            while (stream.Position < stream.Length)
            {
                var chunkId = new string(reader.ReadChars(4));
                var chunkSize = reader.ReadInt32();

                switch (chunkId)
                {
                    case "fmt ":
                        {
                            audioFormat = reader.ReadUInt16();
                            channels = reader.ReadUInt16();
                            sampleRate = reader.ReadUInt32();
                            byteRate = reader.ReadUInt32();
                            blockAlign = reader.ReadUInt16();
                            bitsPerSample = reader.ReadUInt16();

                            if (chunkSize > 16)
                            {
                                reader.ReadBytes(chunkSize - 16);
                            }

                            break;
                        }

                    case "data":
                        {
                            var sampleCount = chunkSize / sizeof(short);

                            for (var i = 0; i < sampleCount; i++)
                            {
                                samples.Add(reader.ReadInt16());
                            }

                            break;
                        }

                    default:
                        {
                            reader.ReadBytes(chunkSize);
                            break;
                        }
                }
            }

            if (audioFormat != 1)
            {
                throw new NotSupportedException("Поддерживается только PCM WAV.");
            }

            if (bitsPerSample != 16)
            {
                throw new NotSupportedException("Поддерживаются только 16-битные WAV.");
            }
                
            return new WavData
            {
                AudioFormat = audioFormat,
                Channels = channels,
                SampleRate = sampleRate,
                BitsPerSample = bitsPerSample,
                BlockAlign = blockAlign,
                ByteRate = byteRate,
                Samples = [.. samples]
            };
        }

        public static void WriteWavFile(string file, WavData wav)
        {
            using var stream = File.Create(file);
            using var writer = new BinaryWriter(stream);

            var dataSize = (uint)(wav.Samples.Length * sizeof(short));
            const uint fmtSize = 16;

            writer.Write("RIFF".ToCharArray());

            var fileSize = 4u + 8u + fmtSize + 8u + dataSize;

            writer.Write(fileSize);

            writer.Write("WAVE".ToCharArray());


            writer.Write("fmt ".ToCharArray());
            writer.Write(fmtSize);

            writer.Write(wav.AudioFormat);
            writer.Write(wav.Channels);
            writer.Write(wav.SampleRate);
            writer.Write(wav.ByteRate);
            writer.Write(wav.BlockAlign);
            writer.Write(wav.BitsPerSample);

            writer.Write("data".ToCharArray());
            writer.Write(dataSize);

            foreach (var sample in wav.Samples)
            {
                writer.Write(sample);
            }
        }
    }
}

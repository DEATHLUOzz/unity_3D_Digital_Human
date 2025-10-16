using System;
using UnityEngine;

public static class WavUtility
{
    public static AudioClip ToAudioClip(byte[] wavData, int offsetSamples = 0, string name = "WavAudio")
    {
        if (wavData == null || wavData.Length < 44)
        {
            Debug.LogError("音频数据无效或过短！");
            return null;
        }

        try
        {
            int channels = BitConverter.ToInt16(wavData, 22);
            int sampleRate = BitConverter.ToInt32(wavData, 24);
            int pos = 44;

            int samples = (wavData.Length - pos) / 2 / channels;
            if (samples <= 0)
            {
                Debug.LogError($"样本数异常: samples={samples}, channels={channels}, dataLength={wavData.Length}");
                return null;
            }

            float[] floatData = new float[samples * channels];
            int i = 0;
            while (pos + 1 < wavData.Length && i < floatData.Length)
            {
                short sample = BitConverter.ToInt16(wavData, pos);
                floatData[i] = sample / 32768.0f;
                pos += 2;
                i++;
            }

            AudioClip clip = AudioClip.Create(name, samples, channels, sampleRate, false);
            clip.SetData(floatData, offsetSamples);
            return clip;
        }
        catch (Exception e)
        {
            Debug.LogError("解析 WAV 出错: " + e.Message);
            return null;
        }
    }
}



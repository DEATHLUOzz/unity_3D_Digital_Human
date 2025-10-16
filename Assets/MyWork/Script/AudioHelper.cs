using UnityEngine;
using System;

public static class AudioHelper
{
    public static AudioClip CreateAudioClip(byte[] wav)
    {
        if (wav == null || wav.Length < 44)
        {
            Debug.LogError("⚠️ 音频数据为空或长度不足");
            return null;
        }

        int channels = BitConverter.ToInt16(wav, 22);
        int frequency = BitConverter.ToInt32(wav, 24);
        int pos = 44;
        int sampleCount = (wav.Length - pos) / 2 / channels;

        if (sampleCount <= 0)
        {
            Debug.LogError($"⚠️ 音频样本数为0, wav.Length={wav.Length}, channels={channels}");
            return null;
        }

        float[] leftChannel = new float[sampleCount];
        int i = 0;
        while (pos + 1 < wav.Length && i < sampleCount)
        {
            leftChannel[i] = BytesToFloat(wav[pos], wav[pos + 1]);
            pos += 2 * channels;
            i++;
        }

        AudioClip clip = AudioClip.Create("TTS_Audio", sampleCount, channels, frequency, false);
        clip.SetData(leftChannel, 0);
        return clip;
    }

    private static float BytesToFloat(byte firstByte, byte secondByte)
    {
        short s = (short)((secondByte << 8) | firstByte);
        return s / 32768f;
    }
}






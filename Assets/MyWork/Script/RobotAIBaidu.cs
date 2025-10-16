using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class RobotAIBaiduDialog : MonoBehaviour
{
    [Header("百度API配置")]
    public string apiKey = "EadUO9NBVyjblRRt4i3TXbr8";
    public string secretKey = "LxdB2EXgEGbrmywYYIQr9biSdJQb86oA";
    private string accessToken = "";

    [Header("录音设置")]
    public int sampleRate = 16000;
    private AudioSource audioSource;
    private bool isRecording = false;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        StartCoroutine(GetAccessToken());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && !isRecording)
        {
            StartCoroutine(RecordAndChat());
        }
    }

    IEnumerator GetAccessToken()
    {
        string url = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={apiKey}&client_secret={secretKey}";
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = req.downloadHandler.text;
            int start = json.IndexOf("access_token") + 15;
            int end = json.IndexOf("\"", start);
            accessToken = json.Substring(start, end - start);
            Debug.Log("✅ Token 获取成功");
        }
        else
        {
            Debug.LogError("❌ 获取Token失败：" + req.error);
        }
    }

    IEnumerator RecordAndChat()
    {
        isRecording = true;
        Debug.Log("🎙️ 开始录音...");
        AudioClip clip = Microphone.Start(null, false, 3, sampleRate);
        yield return new WaitForSeconds(3);
        Microphone.End(null);
        Debug.Log("🛑 录音结束，正在识别...");

        // 转为PCM16
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);
        byte[] pcmBytes = ConvertAudioToPCM16(samples);
        string base64Audio = Convert.ToBase64String(pcmBytes);

        // 语音识别
        string asrUrl = "https://vop.baidu.com/server_api";
        string asrJson = $"{{\"format\":\"pcm\",\"rate\":16000,\"channel\":1,\"token\":\"{accessToken}\",\"cuid\":\"unity_ai\",\"speech\":\"{base64Audio}\",\"len\":{pcmBytes.Length}}}";

        UnityWebRequest asrReq = new UnityWebRequest(asrUrl, "POST");
        asrReq.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(asrJson));
        asrReq.downloadHandler = new DownloadHandlerBuffer();
        asrReq.SetRequestHeader("Content-Type", "application/json");
        yield return asrReq.SendWebRequest();

        if (asrReq.result == UnityWebRequest.Result.Success)
        {
            string res = asrReq.downloadHandler.text;
            if (res.Contains("\"result\""))
            {
                int s = res.IndexOf("[\"") + 2;
                int e = res.IndexOf("\"]");
                string userText = res.Substring(s, e - s);
                Debug.Log("👂 用户说：" + userText);

                yield return StartCoroutine(GetAIReply(userText));
            }
            else
            {
                Debug.LogWarning("⚠️ 识别失败：" + res);
            }
        }
        else
        {
            Debug.LogError("❌ 语音识别失败：" + asrReq.error);
        }

        isRecording = false;
    }

   IEnumerator GetAIReply(string userText)
{
    // ✅ 百度文心对话接口
    string chatUrl = $"https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat/completions?access_token={accessToken}";
    string postData = "{\"messages\":[{\"role\":\"user\",\"content\":\"" + userText + "\"}]}";

    UnityWebRequest chatReq = new UnityWebRequest(chatUrl, "POST");
    chatReq.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(postData));
    chatReq.downloadHandler = new DownloadHandlerBuffer();
    chatReq.SetRequestHeader("Content-Type", "application/json");

    Debug.Log("📤 发送用户输入到百度AI：" + userText);
    yield return chatReq.SendWebRequest();

    if (chatReq.result == UnityWebRequest.Result.Success)
    {
        string res = chatReq.downloadHandler.text;
        Debug.Log("📩 收到AI原始回复：" + res);

        // ✅ 尝试安全提取 "result" 字段（不容易崩）
        string reply = "";
        int s = res.IndexOf("\"result\":");
        if (s != -1)
        {
            s = res.IndexOf("\"", s + 9) + 1;
            int e = res.IndexOf("\"", s);
            if (e > s) reply = res.Substring(s, e - s);
        }

        if (!string.IsNullOrEmpty(reply))
        {
            Debug.Log("🤖 AI回复：" + reply);
            StartCoroutine(PlayTTS(reply));  // ✅ 播放语音
        }
        else
        {
            Debug.LogWarning("⚠️ AI未返回有效内容：" + res);
        }
    }
    else
    {
        Debug.LogError("❌ 请求AI失败：" + chatReq.error);
    }
}


IEnumerator PlayTTS(string text)
{
    // ✅ 使用百度语音合成接口（不是 chatUrl）
    string ttsUrl = $"https://tsn.baidu.com/text2audio?tex={UnityWebRequest.EscapeURL(text)}&lan=zh&cuid=UnityAIClient&ctp=1&tok={accessToken}";

    using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(ttsUrl, AudioType.WAV))
    {
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            audioSource.PlayOneShot(clip);
            Debug.Log("🔊 AI语音：" + text);
        }
        else
        {
            Debug.LogError("❌ TTS失败：" + req.error);
        }
    }
}


    byte[] ConvertAudioToPCM16(float[] samples)
    {
        byte[] bytes = new byte[samples.Length * 2];
        int i = 0;
        foreach (float sample in samples)
        {
            short intSample = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
            bytes[i++] = (byte)(intSample & 0xFF);
            bytes[i++] = (byte)((intSample >> 8) & 0xFF);
        }
        return bytes;
    }
}





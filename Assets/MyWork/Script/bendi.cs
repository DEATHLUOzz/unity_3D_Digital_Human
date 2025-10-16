using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class RobotLocalAudioBaiduChat : MonoBehaviour
{
    [Header("百度API配置")]
    public string apiKey = "EadUO9NBVyjblRRt4i3TXbr8";
    public string secretKey = "LxdB2EXgEGbrmywYYIQr9biSdJQb86oA";
    private string accessToken = "";

    [Header("录音设置")]
    public int sampleRate = 16000;
    private AudioSource audioSource;
    private AudioClip recordedClip;
    private bool isRecording = false;

    [Header("AI思考延迟设置")]
    // 每个问题独立延迟（秒）
    public float thinkHello = 1.0f;
    public float thinkIntro = 2.0f;
    public float thinkFront = 1.5f;
    public float thinkWho = 2.3f;
    public float thinkPacking = 3.0f;
    public float thinkWhySix = 5.0f;
    public float thinkTwoAxis = 5.0f;
    public float thinkUnknown = 1.8f;

    [Header("本地问答音频")]
    public AudioClip clipHello;
    public AudioClip clipIntro;
    public AudioClip clipFront;
    public AudioClip clipWho;
    public AudioClip clipPacking;
    public AudioClip clipWhySixAxis;
    public AudioClip clipTwoAxis;
    public AudioClip clipUnknown;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        StartCoroutine(GetAccessToken());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!isRecording)
            {
                StartRecording();
            }
            else
            {
                StopRecordingAndRecognize();
            }
        }
    }

    void StartRecording()
    {
        recordedClip = Microphone.Start(null, false, 5, sampleRate);
        isRecording = true;
    }

    void StopRecordingAndRecognize()
    {
        if (!isRecording) return;

        Microphone.End(null);
        isRecording = false;
        StartCoroutine(SendToBaiduASR(recordedClip));
    }

    IEnumerator GetAccessToken()
    {
        LogChat("🔗 系统", "正在连接百度服务器...");

        string url = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={apiKey}&client_secret={secretKey}";
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string json = req.downloadHandler.text;

            if (json.Contains("access_token"))
            {
                int start = json.IndexOf("access_token") + 15;
                int end = json.IndexOf("\"", start);
                accessToken = json.Substring(start, end - start);

                LogChat("✅ 系统", "已成功连接百度服务器，AccessToken 获取成功。");
            }
            else
            {
                LogChat("⚠️ 系统", "连接成功，但未能正确解析 AccessToken，请检查API Key与Secret Key。");
            }
        }
        else
        {
            LogChat("❌ 系统", "连接百度服务器失败，请检查网络或API Key配置。");
        }
    }


    IEnumerator SendToBaiduASR(AudioClip clip)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);
        byte[] pcmBytes = ConvertAudioToPCM16(samples);
        string base64Audio = System.Convert.ToBase64String(pcmBytes);

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
            string userText = ParseASRResult(res);
            if (!string.IsNullOrEmpty(userText))
            {
                LogChat("👤 用户", userText);
                yield return StartCoroutine(PlayLocalAudio(userText));
            }
            else
            {
                LogChat("👤 用户", "（未识别到语音）");
                LogChat("🤖 AI", "抱歉，我没听清，可以再说一遍吗？");
            }
        }
        else
        {
            LogChat("🤖 AI", "网络有点问题，稍后再试吧。");
        }
    }

    string ParseASRResult(string json)
    {
        if (json.Contains("\"result\""))
        {
            int s = json.IndexOf("[\"") + 2;
            int e = json.IndexOf("\"]");
            return json.Substring(s, e - s);
        }
        return "";
    }

    IEnumerator PlayLocalAudio(string userText)
    {
        AudioClip clipToPlay = clipUnknown;
        string aiReply = "我没听清楚请重复一遍";
        float thinkTime = thinkUnknown; // 默认思考时间

        if (userText.Contains("你好"))
        {
            clipToPlay = clipHello;
            aiReply = "你好，我是你的AI导游~";
            thinkTime = thinkHello;
        }
        else if (userText.Contains("用英语回答我，这里是干什么的") || userText.Contains("你是干什么的"))
        {
            clipToPlay = clipIntro;
            aiReply = "This is the packing area of the smart factory.";
            thinkTime = thinkIntro;
        }
        else if (userText.Contains("好的，我没什么问题了"))
        {
            clipToPlay = clipFront;
            aiReply = "那么接下来让我们近距离观看作业吧，拜拜";
            thinkTime = thinkFront;
        }
        else if (userText.Contains("你是谁"))
        {
            clipToPlay = clipWho;
            aiReply = "我是你的智能导览助手，很高兴为你服务。";
            thinkTime = thinkWho;
        }
        else if (userText.Contains("打包的流程是什么"))
        {
            clipToPlay = clipPacking;
            aiReply = "六轴机械臂负责两端流转，二轴桁架机械手专注中间传输，让打包流程连贯高效。";
            thinkTime = thinkPacking;
        }
        else if (userText.Contains("为什么要用六轴机械臂"))
        {
            clipToPlay = clipWhySixAxis;
            aiReply = "六轴机械臂的优势是灵活度高，能像手臂一样做多方向、多角度的复杂动作。不管是把打包盒精准送到传送带旁，还是把成品放到不同指定位置，多轴配合都能实现精准定位，适应多样的搬运需求，低轴数机械臂很难做到这么灵活。";
            thinkTime = thinkWhySix;
        }
        else if (userText.Contains("结构像塔吊的东西是什么")||userText.Contains("结构向塔吊的东西是什么"))
        {
            clipToPlay = clipTwoAxis;
            aiReply = "这个是二轴桁架机械手，二轴桁架机械手结构简单，靠水平、垂直两个直线轴运动，像在传送带和打包盒间走 “直线捷径”。它不仅能精准对准物品和盒口，直线传动还让它速度快、刚性好，适合这种 “点对点” 的高频取放，而且能定制行程，成本也更可控。";
            thinkTime = thinkTwoAxis;
        }

        // 模拟“AI思考中”
        LogChat("🤖 AI", "（思考中...）");
        yield return new WaitForSeconds(thinkTime);

        audioSource.PlayOneShot(clipToPlay);
        LogChat("🤖 AI", aiReply);
    }

    void LogChat(string speaker, string message)
    {
        Debug.Log($"[{System.DateTime.Now:HH:mm:ss}] {speaker}：{message}");
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








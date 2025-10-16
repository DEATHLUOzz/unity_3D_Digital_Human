using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class RobotGuideController : MonoBehaviour
{
    [Header("火山引擎 API 配置")]
    public string appId = "3581905229";
    public string accessToken = "9ox0kcQlQUMlBwpQn8YMbHgaQYlQzb8K";
    public string TTS_API_URL = "https://openspeech.bytedance.com/api/v3/tts/unidirectional ";

    [Header("移动配置")]
    public float moveSpeed = 2f;
    public float turnSpeed = 90f;
    private bool isStopped = false;

    [Header("讲解目标")]
    public Transform lineTarget;  // 流水线位置
    public Transform otherTarget; // 其他讲解目标

    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        HandleKeyboardMovement();
        HandleKeyboardCommands();
    }

    // ======== WASD 控制移动 ========
    void HandleKeyboardMovement()
    {
        if (isStopped) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        transform.Translate(Vector3.forward * v * moveSpeed * Time.deltaTime);
        transform.Rotate(Vector3.up, h * turnSpeed * Time.deltaTime);
    }

    // ======== 键盘命令处理 ========
    void HandleKeyboardCommands()
    {
        // 空格键停下
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StopMovement();
            Debug.Log("机器人已停下，等待讲解...");
        }

        // 数字1键：流水线讲解
        if (isStopped && Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (lineTarget != null)
            {
                FaceTarget(lineTarget);
            }
            StartCoroutine(PlayTTS("这是流水线讲解示例：这里有三个传送带，依次将零件输送到工作站。"));
        }

        // 数字2键：其他讲解
        if (isStopped && Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (otherTarget != null)
            {
                FaceTarget(otherTarget);
            }
            StartCoroutine(PlayTTS("这里是其他设备示意讲解，大家可以看到控制柜和机器人手臂。"));
        }

        // 数字3键：继续移动
        if (isStopped && Input.GetKeyDown(KeyCode.Alpha3))
        {
            ResumeMovement();
        }
    }

    // ======== 面向目标 ========
    void FaceTarget(Transform target)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; // 只旋转水平面
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void StopMovement() => isStopped = true;
    void ResumeMovement() => isStopped = false;

    // ======== 调用火山引擎 TTS ========
    IEnumerator PlayTTS(string text)
    {
        string jsonData = "{\"text\":\"" + text + "\"}";
        UnityWebRequest request = new UnityWebRequest(TTS_API_URL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("X-Appid", appId);
        request.SetRequestHeader("X-Token", accessToken);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] audioData = request.downloadHandler.data;
            AudioClip clip = WavUtility.ToAudioClip(audioData, 0, "TTS");
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogError("TTS请求失败：" + request.error);
        }
    }
}


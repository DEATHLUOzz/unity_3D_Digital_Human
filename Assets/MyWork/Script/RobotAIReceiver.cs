using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;

public class RobotAIReceiver : MonoBehaviour
{
    public int Port = 8080;
    public TextMeshProUGUI displayText; // TextMeshPro 显示文本
    private TcpListener listener;
    private Thread listenerThread;

    void Awake()
    {
        // 开启 TCP 监听线程
        listenerThread = new Thread(StartListener);
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    void OnDestroy()
    {
        try
        {
            listener?.Stop();
            listenerThread?.Abort();
        }
        catch { }
    }

    private void StartListener()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            Debug.Log($"✅ TCP 监听已启动，端口 {Port}");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(HandleClient, client);
            }
        }
        catch (ThreadAbortException)
        {
            Debug.LogWarning("TCP监听线程已终止");
        }
        catch (Exception e)
        {
            Debug.LogError("❌ TCP监听出错: " + e);
        }
    }

    private void HandleClient(object obj)
    {
        TcpClient client = obj as TcpClient;
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024 * 16];
            int bytesRead;
            StringBuilder sb = new StringBuilder();

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                string msg = sb.ToString();
                if (msg.Contains("<END>"))
                {
                    string fullMsg = msg.Replace("<END>", "");
                    sb.Clear();
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        HandleMessage(fullMsg);
                    });
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("❌ TCP客户端处理出错: " + e);
        }
        finally
        {
            client.Close();
        }
    }

    private void HandleMessage(string msg)
    {
        try
        {
            string[] parts = msg.Split('|');
            if (parts.Length != 2)
            {
                Debug.LogError("⚠️ 消息格式错误");
                return;
            }

            string text = parts[0];
            string audioBase64 = parts[1];

            if (displayText != null)
                displayText.text = text;

            byte[] audioBytes = Convert.FromBase64String(audioBase64);
            PlayAudio(audioBytes);
        }
        catch (Exception e)
        {
            Debug.LogError("❌ 处理客户端数据出错: " + e);
        }
    }

    private void PlayAudio(byte[] audioBytes)
    {
        try
        {
            AudioClip clip = AudioHelper.CreateAudioClip(audioBytes);
            if (clip == null)
            {
                Debug.LogError("❌ 音频解码失败");
                return;
            }

            GameObject audioObj = new GameObject("TTS_AudioPlayer");
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.Play();
            Destroy(audioObj, clip.length + 0.1f);
        }
        catch (Exception e)
        {
            Debug.LogError("❌ 播放音频出错: " + e);
        }
    }
}















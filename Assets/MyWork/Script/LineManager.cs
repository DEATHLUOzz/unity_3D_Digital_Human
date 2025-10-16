using UnityEngine;
using System.Collections;

/// <summary>
/// 最终整合版流水线管理脚本：
/// - 复制原始流水线为多条
/// - 每条流水线挂载安全出料脚本 SafeLineController
/// - 出料安全，默认 maxConcurrentSpawn=1, spawnCooldown=1
/// - 可通过代码手动启动/停止
/// </summary>
public class LineManager : MonoBehaviour
{
    [Header("原始流水线对象（Hierarchy 中已有的流水线）")]
    public GameObject originalLine;

    [Header("复制设置")]
    public int duplicateCount = 3;                  // 总共生成几条流水线
    public Vector3 offset = new Vector3(5f, 0f, 0f); // 每条流水线间隔

    [Header("出料设置（安全）")]
    public GameObject itemPrefab;       // 出料物体
    public float spawnCooldown = 1f;    // 出料间隔
    public int maxConcurrentSpawn = 1;  // 同时最大出料数量

    void Start()
    {
        if (originalLine == null)
        {
            Debug.LogError("请在 Inspector 指定原始流水线 Original Line");
            return;
        }

        for (int i = 0; i < duplicateCount; i++)
        {
            Vector3 pos = originalLine.transform.position + i * offset;
            GameObject clone = Instantiate(originalLine, pos, originalLine.transform.rotation);
            clone.name = originalLine.name + "_Copy" + (i + 1);

            // 给每条流水线挂上 SafeLineController
            SafeLineController slc = clone.GetComponent<SafeLineController>();
            if (slc == null)
                slc = clone.AddComponent<SafeLineController>();

            slc.itemPrefab = itemPrefab;
            slc.spawnCooldown = spawnCooldown;
            slc.maxConcurrentSpawn = maxConcurrentSpawn;
        }

        // 原始流水线也挂 SafeLineController
        SafeLineController origSLC = originalLine.GetComponent<SafeLineController>();
        if (origSLC == null)
            origSLC = originalLine.AddComponent<SafeLineController>();

        origSLC.itemPrefab = itemPrefab;
        origSLC.spawnCooldown = spawnCooldown;
        origSLC.maxConcurrentSpawn = maxConcurrentSpawn;
    }
}

/// <summary>
/// 安全版出料控制脚本
/// </summary>
public class SafeLineController : MonoBehaviour
{
    [Header("出料物体预制体")]
    public GameObject itemPrefab;

    [Header("出料位置")]
    public Transform spawnPoint;

    [Header("出料参数")]
    public float spawnCooldown = 1f; // 出料间隔时间
    public int maxConcurrentSpawn = 1; // 同时存在的最大物料数量

    private float lastSpawnTime;
    private int currentItemCount = 0;

    void Start()
    {
        if (spawnPoint == null)
            spawnPoint = this.transform;
    }

    void Update()
    {
        if (itemPrefab == null) return; // 没有 prefab 就不出料

        if (Time.time - lastSpawnTime >= spawnCooldown && currentItemCount < maxConcurrentSpawn)
        {
            SpawnItem();
        }
    }

    void SpawnItem()
    {
        GameObject item = Instantiate(itemPrefab, spawnPoint.position, spawnPoint.rotation);
        currentItemCount++;
        Destroy(item, 10f); // 10 秒后销毁
        lastSpawnTime = Time.time;
    }

    // 手动控制
    public void StartLine() => enabled = true;
    public void StopLine() => enabled = false;
}

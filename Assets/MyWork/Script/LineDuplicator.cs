using UnityEngine;

public class LineDuplicator : MonoBehaviour
{
    [Header("原始流水线")]
    public GameObject originalLine; // 现有可运行流水线

    [Header("复制设置")]
    public int duplicateCount = 3;  // 总共要生成几条
    public Vector3 offset = new Vector3(5f, 0f, 0f); // 每条流水线间隔

    void Start()
    {
        if (originalLine == null)
        {
            Debug.LogError("请在 Inspector 指定 originalLine");
            return;
        }

        for (int i = 0; i < duplicateCount; i++)
        {
            GameObject clone = Instantiate(originalLine, originalLine.transform.position + i * offset, originalLine.transform.rotation);
            clone.name = originalLine.name + "_Copy" + (i + 1);

            // 确保每条流水线都挂 SafeLineController
            SafeLineController slc = clone.GetComponent<SafeLineController>();
            if (slc == null)
                slc = clone.AddComponent<SafeLineController>();

            // 初始化安全参数
            slc.maxConcurrentSpawn = 1;
            slc.spawnCooldown = 1f;
        }

        // 原始流水线也加上 SafeLineController
        SafeLineController origSLC = originalLine.GetComponent<SafeLineController>();
        if (origSLC == null)
            origSLC = originalLine.AddComponent<SafeLineController>();
        origSLC.maxConcurrentSpawn = 1;
        origSLC.spawnCooldown = 1f;
    }
}

using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;       // 要跟随的角色
    public Vector3 offset = new Vector3(0, 3, -5); // 相对于角色的偏移
    public float smoothSpeed = 5f; // 平滑跟随速度

    void LateUpdate()
    {
        if(target == null) return;

        // 目标位置 = 角色位置 + 偏移
        Vector3 desiredPosition = target.position + offset;

        // 平滑插值
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // 让摄像机看向角色
        transform.LookAt(target.position + Vector3.up * 1.5f); // 看向角色头部
    }
}

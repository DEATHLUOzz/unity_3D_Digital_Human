using UnityEngine;

public class TalkOnKeyPress : MonoBehaviour
{
    public Animator animator;     // 模型的 Animator
    public string talkParam = "isTalking"; // Animator 的 Bool 参数名

    void Update()
    {
        // 按下 O 键
        if (Input.GetKeyDown(KeyCode.O))
        {
            animator.SetBool(talkParam, true);
        }

        // 松开 O 键
        if (Input.GetKeyUp(KeyCode.O))
        {
            animator.SetBool(talkParam, false);
        }
    }
}

using System.ComponentModel;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private float crossFadeDuration = 0.1f;

    private Animator animator;
    private string currentAnimation = "";

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayAnimation(string name)
    {
        if (animator == null) return;
        if (currentAnimation == name) return;

        currentAnimation = name;
        animator.CrossFade(name, crossFadeDuration);
    }

    public float GetAnimationLength(string name)
    {
        if (animator == null) return 0f;

        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0)
        {
            return clipInfo[0].clip.length;
        }

        return 0f;
    }

}

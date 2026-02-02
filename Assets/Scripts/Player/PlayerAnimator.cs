using System.ComponentModel;
using UnityEngine;

public class PlayerAnimator : Singleton<PlayerAnimator>
{
    [SerializeField] private float crossFadeDuration = 0.1f;

    private Animator animator;
    private string currentAnimation = "";

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayAnimation(string name, bool forceRestart = false)
    {
        if (animator == null) return;

        if (forceRestart)
        {
            animator.Play(name, 0, 0f);
            currentAnimation = name;
            return;
        }

        if (currentAnimation == name) return;

        currentAnimation = name;
        animator.CrossFade(name, crossFadeDuration);
    }

    public float GetAnimationLength(string name)
    {
        if (animator == null) return 0f;

        RuntimeAnimatorController ac = animator.runtimeAnimatorController;
        for (int i = 0; i < ac.animationClips.Length; i++)
        {
            if (ac.animationClips[i].name == name)
            {
                return ac.animationClips[i].length;
            }
        }

        return 0f;
    }

    public string GetCurrentAnimationName()
    {
        return currentAnimation;
    }

}

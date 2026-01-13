using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public static PlayerAnimator Instance { get; private set; }

    private Animator animator;
    [SerializeField] private float crossFadeDuration = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void PlayAnimation(string name)
    {
        if (animator == null) return;
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

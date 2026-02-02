using UnityEngine;

public class PartnerController : Singleton<PartnerController>
{
    //================================ Fields =================================//
    private Animator animator;

    //================================ Unity Methods =================================//
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    //================================ Public Methods =================================//
    public Vector3 PartnerLocation()
    {
        return Instance.transform.position;
    }

    public void InitiateDance()
    {
        animator.CrossFade("Initiate", 0.1f);
    }

    public void ReturnToIdle()
    {
        animator.CrossFade("Idle", 0.1f);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void Activate()
    {
        gameObject.SetActive(true);
    }

}

using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class Collectible : MonoBehaviour
{
    [SerializeField] private GrabHitbox grabHitbox;

    private void OnEnable()
    {
        if (grabHitbox != null)
            grabHitbox.onCollectibleGrabbed += OnCollected;
    }

    private void OnDisable()
    {
        if (grabHitbox != null)
            grabHitbox.onCollectibleGrabbed -= OnCollected;
    }

    private void OnCollected()
    {
        gameObject.SetActive(false);
    }
}
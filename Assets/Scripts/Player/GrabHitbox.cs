using System;
using Unity.VisualScripting;
using UnityEngine;

public class GrabHitbox : MonoBehaviour
{
    public event Action onCollectibleGrabbed;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("GrabHitbox triggered by: " + collision.name);
        if (collision.CompareTag("Collectible"))
        {
            Debug.Log("Collectible grabbed!");
            onCollectibleGrabbed?.Invoke();
        }
    }
}

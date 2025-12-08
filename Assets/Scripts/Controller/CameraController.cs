using UnityEngine;

public class CameraController : MonoBehaviour
{
    private Transform player; // Reference to the player's transform
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(player.position.x, transform.position.y, transform.position.z);   
    }
}

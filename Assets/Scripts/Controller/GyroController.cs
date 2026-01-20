using UnityEngine;
using System.Collections;

public class GyroController : MonoBehaviour
{
    private int[] deviceHandles;
    private int connectedDevices;

    // Jump config
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] float gyroXTriggerThreshold = 400f; // adjust for your device's scale
    [SerializeField] float jumpCooldown = 0.35f;
    [SerializeField] private float gyroActionCooldown = 0.35f;

    private bool canTriggerJump = true;
    private bool canTriggerGyroAction = true;

    void Start()
    {
        // Connect devices
        connectedDevices = JSL.JslConnectDevices();
        deviceHandles = new int[connectedDevices];

        int count = JSL.JslGetConnectedDeviceHandles(deviceHandles, connectedDevices);
        Debug.Log($"Connected {count} device(s)");

        if (count == 0)
        {
            Debug.LogWarning("No controllers detected!");
        }
    }

    void Update()
    {
        for (int i = 0; i < connectedDevices; i++)
        {
            int handle = deviceHandles[i];

            // Get IMU state
            JSL.IMU_STATE imu = JSL.JslGetIMUState(handle);

            // Debug log accelerometer and gyro
            //Debug.Log($"Device {handle} - Accel: X={imu.accelX:F2}, Y={imu.accelY:F2}, Z={imu.accelZ:F2} | Gyro: X={imu.gyroX:F2}, Y={imu.gyroY:F2}, Z={imu.gyroZ:F2}");

            if (canTriggerGyroAction && Mathf.Abs(imu.gyroX) >= gyroXTriggerThreshold)
            {
                TriggerGyroAction();
                break;
            }

            // Jump is now handled by right stick (OnLook); gyro input no longer triggers jump.
        }
    }

    private void TriggerJump()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        StartCoroutine(JumpCooldown());
    }

    private void TriggerGyroAction()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        //playerMovement.TriggerGyroDash();
        StartCoroutine(GyroActionCooldown());
    }

    private IEnumerator JumpCooldown()
    {
        canTriggerJump = false;
        yield return new WaitForSeconds(jumpCooldown);
        canTriggerJump = true;
    }

    private IEnumerator GyroActionCooldown()
    {
        canTriggerGyroAction = false;
        yield return new WaitForSeconds(gyroActionCooldown);
        canTriggerGyroAction = true;
    }

    private void OnApplicationQuit()
    {
        JSL.JslDisconnectAndDisposeAll();
    }
}

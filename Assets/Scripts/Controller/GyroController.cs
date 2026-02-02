using UnityEngine;
using System.Collections;

public class GyroController : MonoBehaviour
{
    private int[] deviceHandles;
    private int connectedDevices;

    // Jump config
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] float gyroXTriggerThreshold = 400f;
    [SerializeField] float jumpCooldown = 0.35f;
    [SerializeField] private float gyroActionCooldown = 0.5f; // Increase default cooldown

    [SerializeField] float gyroUpThreshold = 400f;
    [SerializeField] float gyroSideThreshold = 400f;

    // Rotation tracking
    [SerializeField] float targetRotationDegrees = 50f;
    [SerializeField] float maxTimeWindow = 0.5f;

    private float accumulatedRotationUp = 0f;
    private float rotationStartTime = 0f;
    private bool isTrackingRotation = false;

    private bool canTriggerJump = true;
    private bool canTriggerGyroAction = true;
    private bool combo3Triggered = false;
    private bool combo4Triggered = false;

    // Input Direction
    private bool lastGyroSideWasRight = false;

    void Start()
    {
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
            JSL.IMU_STATE imu = JSL.JslGetIMUState(handle);

            // Debug to see actual values
            // Debug.Log($"Gyro - X: {imu.gyroX}, Y: {imu.gyroY}, Z: {imu.gyroZ}");

            float angularVelocityUp = Mathf.Abs(imu.gyroX);
            float angularVelocitySide = Mathf.Abs(imu.gyroY);

            // Gyro UP detection
            if (canTriggerGyroAction && angularVelocityUp >= gyroUpThreshold)
            {
                if (!isTrackingRotation)
                {
                    isTrackingRotation = true;
                    accumulatedRotationUp = 0f;
                    rotationStartTime = Time.time;
                }

                accumulatedRotationUp += angularVelocityUp * Time.deltaTime;

                if (accumulatedRotationUp >= targetRotationDegrees)
                {
                    Debug.Log($"Gyro UP triggered! Rotation: {accumulatedRotationUp}° in {Time.time - rotationStartTime}s");
                    combo3Triggered = true;
                    ResetRotationTracking();
                    StartCoroutine(GyroActionCooldown());
                    continue; // Use continue instead of break
                }
            }
            else if (isTrackingRotation && Time.time - rotationStartTime > maxTimeWindow)
            {
                ResetRotationTracking();
            }

            // Gyro SIDE detection - now independent
            if (canTriggerGyroAction && angularVelocitySide >= gyroSideThreshold)
            {
                Debug.Log($"Gyro SIDE triggered! Value: {angularVelocitySide}");
                combo4Triggered = true;
                lastGyroSideWasRight = (imu.gyroY < 0);
                StartCoroutine(GyroActionCooldown());
                continue;
            }
        }
    }

    private void ResetRotationTracking()
    {
        isTrackingRotation = false;
        accumulatedRotationUp = 0f;
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

    public bool IsGyroUpDetected()
    {
        if (combo3Triggered)
        {
            combo3Triggered = false;
            return true;
        }
        return false;
    }

    public bool IsGyroSideDetected()
    {
        if (combo4Triggered)
        {
            combo4Triggered = false;
            return true;
        }
        return false;
    }

    public bool WasLastGyroSideRight()
    {
        return lastGyroSideWasRight;
    }

    /// <summary>
    /// Clears any pending gyro triggers (useful when combo fails/resets)
    /// </summary>
    public void ClearPendingTriggers()
    {
        combo3Triggered = false;
        combo4Triggered = false;
    }
}

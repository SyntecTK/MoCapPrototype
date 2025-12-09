using UnityEngine;

public class GyroReader : MonoBehaviour
{
    private int[] deviceHandles;
    private int connectedDevices;

    // Jump config
    [SerializeField] float gyroXTriggerThreshold = 400f; // adjust for your device's scale
    [SerializeField] float jumpHeight = 1.0f;
    [SerializeField] float jumpDuration = 0.35f; // short jump
    [SerializeField] float spinDuration = 0.35f; // spin during jump
    [SerializeField] float spinSpeedDegreesPerSecond = 720f; // increase this to spin faster

    bool isJumping;

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
            Debug.Log($"Device {handle} - Accel: X={imu.accelX:F2}, Y={imu.accelY:F2}, Z={imu.accelZ:F2} | Gyro: X={imu.gyroX:F2}, Y={imu.gyroY:F2}, Z={imu.gyroZ:F2}");

            // Trigger jump when rotating fast around gyroX
            if (!isJumping && Mathf.Abs(imu.gyroX) >= gyroXTriggerThreshold)
            {
                StartCoroutine(JumpAndSpin());
                // Trigger from first device only; remove this break to allow any device trigger
                break;
            }
        }
    }

    System.Collections.IEnumerator JumpAndSpin()
    {
        isJumping = true;

        Vector3 startPos = transform.localPosition;
        Vector3 apexPos = startPos + Vector3.up * jumpHeight;

        float elapsed = 0f;
        float half = jumpDuration * 0.5f;
        float spunDegrees = 0f;

        // Up phase
        while (elapsed < half)
        {
            float t = elapsed / half;
            transform.localPosition = Vector3.Lerp(startPos, apexPos, t);

            // Spin faster using spin speed
            spunDegrees = Mathf.Min(360f, spunDegrees + spinSpeedDegreesPerSecond * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0f, spunDegrees, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Down phase
        elapsed = 0f;
        while (elapsed < half)
        {
            float t = elapsed / half;
            transform.localPosition = Vector3.Lerp(apexPos, startPos, t);

            spunDegrees = Mathf.Min(360f, spunDegrees + spinSpeedDegreesPerSecond * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0f, spunDegrees, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure exact final rotation and position
        transform.localPosition = startPos;
        transform.localRotation = Quaternion.Euler(0f, 360f, 0f);
        transform.localRotation = Quaternion.identity;

        isJumping = false;
    }

    void OnApplicationQuit()
    {
        JSL.JslDisconnectAndDisposeAll();
    }
}

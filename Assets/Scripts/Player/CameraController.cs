using System.Collections;
using UnityEngine;

// Kamerakontroller som hanterar rotation, zoom, krockjusteringar och effekter
public class CameraController : MonoBehaviour
{
    [Header("--- Target ---")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.65f, 0f); // 🟢 default offset i Y
    [SerializeField] private float smoothTime = 0.1f;

    [Header("--- Orbit Settings ---")]
    [SerializeField] private float distance = 2.5f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 4f;
    [SerializeField] private float yMinLimit = -20f;
    [SerializeField] private float yMaxLimit = 80f;

    [Header("--- Input Settings ---")]
    [SerializeField] private float sensitivity = 1f;

    [Header("--- Collision ---")]
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private float cameraOffsetBuffer = 0.1f;

    private float x = 0f, y = 0f;
    private Vector3 currentVelocity, smoothedTargetPosition, shakeOffset = Vector3.zero;

    private float defaultFOV;
    private Coroutine fovRoutine, shakeRoutine;

    private float targetZoomDistance;
    private Camera cam;

    private InputHandler input;

    void Start()
    {
        InitializeCamera();
        targetZoomDistance = distance;
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleOrbitInput();
        UpdateCameraPosition();
    }

    private void InitializeCamera()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        if (target != null)
        {
            smoothedTargetPosition = target.position + targetOffset;
            input = target.GetComponent<InputHandler>();
        }

        cam = GetComponent<Camera>();
        if (cam != null)
            defaultFOV = cam.fieldOfView;
    }

    private void HandleOrbitInput()
    {
        if (input == null) return;

        // --- Zoom ---
        targetZoomDistance -= input.ScrollInput * zoomSpeed;
        targetZoomDistance = Mathf.Clamp(targetZoomDistance, minDistance, maxDistance);
        distance = Mathf.Lerp(distance, targetZoomDistance, Time.deltaTime / 0.1f);

        // --- Rotation ---
        x += input.LookInput.x * sensitivity;
        y -= input.LookInput.y * sensitivity;
        y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
    }

    private void UpdateCameraPosition()
    {
        Vector3 targetPos = target.position + targetOffset;
        smoothedTargetPosition = Vector3.SmoothDamp(smoothedTargetPosition, targetPos, ref currentVelocity, smoothTime);

        Vector3 desiredOffset = Quaternion.Euler(y, x, 0) * new Vector3(0, 0, -distance);
        float adjustedDistance = GetCollisionAdjustedDistance(desiredOffset);
        Vector3 finalOffset = Quaternion.Euler(y, x, 0) * new Vector3(0, 0, -adjustedDistance);

        transform.position = smoothedTargetPosition + finalOffset + shakeOffset;
        transform.rotation = Quaternion.Euler(y, x, 0);
    }

    private float GetCollisionAdjustedDistance(Vector3 direction)
    {
        Ray ray = new Ray(smoothedTargetPosition, direction.normalized);

        if (Physics.SphereCast(ray, collisionRadius, out RaycastHit hit, distance + cameraOffsetBuffer, collisionLayers))
        {
            return hit.distance - cameraOffsetBuffer;
        }

        return distance;
    }

    // --- Effekter ---
    public void DoFOVPunch(float targetFOV, float duration)
    {
        if (cam == null) return;
        if (fovRoutine != null) StopCoroutine(fovRoutine);
        fovRoutine = StartCoroutine(FOVEffect(targetFOV, duration));
    }

    private IEnumerator FOVEffect(float targetFOV, float duration)
    {
        float t = 0f;
        float startFOV = cam.fieldOfView;

        while (t < duration)
        {
            cam.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t / duration);
            t += Time.deltaTime;
            yield return null;
        }

        t = 0f;
        while (t < duration)
        {
            cam.fieldOfView = Mathf.Lerp(targetFOV, defaultFOV, t / duration);
            t += Time.deltaTime;
            yield return null;
        }

        cam.fieldOfView = defaultFOV;
    }

    public void DoCameraShake(float intensity, float duration)
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(CameraShake(intensity, duration));
    }

    private IEnumerator CameraShake(float intensity, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            shakeOffset = Random.insideUnitSphere * intensity;
            shakeOffset.z = 0;
            t += Time.deltaTime;
            yield return null;
        }

        shakeOffset = Vector3.zero;
    }
}

using Dreamteck.Splines;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private SplineComputer spline;

    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float accelerationRate = 18f;
    [SerializeField] private float decelerationRate = 8f;

    [SerializeField] private float rotationSpeed = 100f;

    [SerializeField] private float maxLeanAngle = 25f;
    [SerializeField] private float leanSmoothSpeed = 5f;

    private float currentYaw;
    private float currentSpeed;

    private float currentLean;
    private float targetLean;

    private float finalSpeed;

    void Start()
    {
        inputManager.OnMoveContinues += MovementManagement;

        SplineSample sample = new();
        spline.Project(transform.position, ref sample);
        currentYaw = Quaternion.LookRotation(sample.forward).eulerAngles.y;
    }

    private void MovementManagement(Vector2 moveInput)
    {
        if (moveInput.y > 0)
        {
            currentSpeed += accelerationRate * Time.deltaTime;
        }
        else
        {
            currentSpeed -= decelerationRate * Time.deltaTime;
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
        finalSpeed = currentSpeed;
        transform.position += finalSpeed * Time.deltaTime * transform.forward;

        if (currentSpeed > 0.1f)
        {
            currentYaw += moveInput.x * rotationSpeed * Time.deltaTime;
        }

        if (Mathf.Abs(moveInput.x) > 0.01f && currentSpeed > 1f)
        {
            targetLean = -moveInput.x * maxLeanAngle;
        }
        else
        {
            targetLean = 0f;
        }

        currentLean = Mathf.Lerp(currentLean, targetLean, leanSmoothSpeed * Time.deltaTime);

        FollowSplineHeight();
        RestrictInsideTrack();
    }

    private void FollowSplineHeight()
    {
        SplineSample sample = new();
        spline.Project(transform.position, ref sample);

        Vector3 pos = transform.position;
        pos.y = sample.position.y;
        transform.position = pos;

        Quaternion slopeRotation = Quaternion.LookRotation(sample.forward, sample.up);
        Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);

        transform.rotation = yawRotation * Quaternion.Euler(slopeRotation.eulerAngles.x, 0f, currentLean);
    }

    private void RestrictInsideTrack()
    {
        SplineSample sample = new();
        spline.Project(transform.position, ref sample);

        Vector3 offset = transform.position - sample.position;

        float horizontalOffset = Vector3.Dot(offset, sample.right);
        float absOffset = Mathf.Abs(horizontalOffset);
        float edgePercent = Mathf.InverseLerp(3.5f, 4f, absOffset);
        float dragMultiplier = Mathf.Lerp(1f, 0.05f, edgePercent);

        finalSpeed = currentSpeed * dragMultiplier;
        horizontalOffset = Mathf.Clamp(horizontalOffset, -4f, 4f);

        Vector3 finalPosition = sample.position + sample.right * horizontalOffset;
        finalPosition.y = sample.position.y;
        transform.position = finalPosition;
    }

    void OnDestroy()
    {
        inputManager.OnMoveContinues -= MovementManagement;
    }
}

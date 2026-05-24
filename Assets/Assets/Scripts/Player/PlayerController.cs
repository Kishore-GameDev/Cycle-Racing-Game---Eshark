using Dreamteck.Splines;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    public SplineFollower follower;

    [Header("Speed")]
    public float maxSpeed = 70f;
    public float acceleration = 40f;

    [Header("Steering")]
    public float steeringSpeed = 120f;
    public float moveSideSpeed = 6f;
    public float maxRoadOffset = 4f;

    [Header("Height")]
    public float heightOffset = 0.5f;

    public Vector2 moveInput;

    private float currentSpeed;
    private float roadOffset;

    // PLAYER'S OWN ROTATION
    private float currentYaw;

    void Start()
    {
        inputManager.OnMoveContinues += MovementManagement;
    }

    private void MovementManagement(Vector2 moveInput)
    {
        //-----------------------------------
        // INPUT
        //-----------------------------------

        float horizontal = moveInput.x;
        float vertical = Mathf.Max(moveInput.y, 0f);

        //-----------------------------------
        // SPEED
        //-----------------------------------

        float targetSpeed = vertical * maxSpeed;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            acceleration * Time.deltaTime
        );

        follower.followSpeed = currentSpeed;

        //-----------------------------------
        // ROAD OFFSET
        //-----------------------------------

        roadOffset +=
            horizontal * moveSideSpeed * Time.deltaTime;

        roadOffset = Mathf.Clamp(
            roadOffset,
            -maxRoadOffset,
            maxRoadOffset
        );

        //-----------------------------------
        // SPLINE SAMPLE
        //-----------------------------------

        SplineSample sample = follower.result;

        //-----------------------------------
        // POSITION
        //-----------------------------------

        Vector3 finalPos =
            sample.position +
            sample.right * roadOffset +
            sample.up * heightOffset;

        transform.position = finalPos;

        //-----------------------------------
        // MANUAL PLAYER ROTATION
        //-----------------------------------

        currentYaw +=
            horizontal * steeringSpeed * Time.deltaTime;

        Quaternion playerRotation =
            Quaternion.Euler(0f, currentYaw, 0f);

        transform.rotation = playerRotation;
    }

    void OnDestroy()
    {
        inputManager.OnMoveContinues -= MovementManagement;
    }
}

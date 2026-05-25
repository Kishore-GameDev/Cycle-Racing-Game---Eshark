using System.Collections;
using Dreamteck.Splines;
using UnityEngine;

public enum PlayerState
{
    Idle,
    Move,
    Celebration,
    OilCrash,
    MudCrash,
    BarricadeCrash
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private AnimationController animationController;
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
    private float actualDecelerationRate;
    private float actualMaxSpeed;
    private bool oilCrash = false;
    private bool playerCanMove = false;
    private PlayerState playerState;

    private Coroutine oilCrashRoutine = null;
    private Coroutine mudCrashRoutine = null;
    private Coroutine barricadeCrashRoutine = null;

    private Vector3 startPos;
    private Quaternion startRot;
    private float currentRaceCompletePercent;
    private bool registeredRaceComplete = true;

    void Start()
    {
        inputManager.OnMoveContinues += MovementManagement;
        GameManager.StartRace += StartRace;
        GameManager.StopRace += StopRace;
        GameManager.ResetAll += ResetAll;

        actualMaxSpeed = maxSpeed;
        actualDecelerationRate = decelerationRate;

        playerState = PlayerState.Idle;
        UpdateState(PlayerState.Move);

        SplineSample sample = new();
        spline.Project(transform.position, ref sample);
        currentYaw = Quaternion.LookRotation(sample.forward).eulerAngles.y;

        startPos = transform.position;
        startRot = transform.rotation;
    }

    private void StartRace(float raceCompletePercent)
    {
        registeredRaceComplete = false;
        currentRaceCompletePercent = raceCompletePercent;
        UpdateState(PlayerState.Move);
    }

    private void StopRace(bool isPlayerWon)
    {
        if (oilCrashRoutine != null)
        {
            StopCoroutine(oilCrashRoutine);
        }
        if (mudCrashRoutine != null)
        {
            StopCoroutine(mudCrashRoutine);
        }
        if (barricadeCrashRoutine != null)
        {
            StopCoroutine(barricadeCrashRoutine);
        }

        if (isPlayerWon)
        {
            UpdateState(PlayerState.Celebration);
        }
        else
        {
            UpdateState(PlayerState.Idle);
        }
    }

    private void UpdateState(PlayerState _playerState)
    {
        if (playerState == _playerState) return;

        playerState = _playerState;

        switch (playerState)
        {
            case PlayerState.Idle:
                animationController.UpdateCycleAnimState(CycleAnimState.Idle);
                playerCanMove = false;
                break;

            case PlayerState.Move:
                playerCanMove = true;
                break;

            case PlayerState.OilCrash:
                oilCrash = true;
                ResetOilCrashRoutine();
                oilCrashRoutine = StartCoroutine(OilCrash());
                break;

            case PlayerState.MudCrash:
                ResetMudCrashRoutine();
                mudCrashRoutine = StartCoroutine(MudCrash());
                break;

            case PlayerState.BarricadeCrash:
                ResetBarricadeCrashRoutine();
                barricadeCrashRoutine = StartCoroutine(BarricadeCrash());
                break;

            case PlayerState.Celebration:
                playerCanMove = false;
                animationController.UpdateCycleAnimState(CycleAnimState.Celebration);
                break;
        }
    }

    void Update()
    {
        if (IsMovingOppositeDirection())
        {
            Debug.Log("Wrong Way");
        }

        if (!registeredRaceComplete && GetPlayerSplinePercent() >= currentRaceCompletePercent)
        {
            registeredRaceComplete = true;
            GameManager.Instance.RaceCompletedRegister(true);
        }
    }

    private float GetPlayerSplinePercent()
    {
        SplineSample sample = new();

        spline.Project(transform.position, ref sample);

        return (float)sample.percent * 100;
    }

    #region Movement
    private void MovementManagement(Vector2 moveInput)
    {
        if (!playerCanMove)
        {
            return;
        }

        if (oilCrash)
        {
            moveInput.x = 0f;
            moveInput.y = 0f;
        }

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
        if (!oilCrash)
        {
            if (currentSpeed <= 0)
            {
                animationController.UpdateCycleAnimState(CycleAnimState.Idle);
            }
            else
            {
                animationController.UpdateCycleAnimState(CycleAnimState.Move, currentSpeed / maxSpeed);
            }
        }

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
        float edgePercent = Mathf.InverseLerp(3f, 4f, absOffset);
        float dragMultiplier = Mathf.Lerp(1f, 0.000001f, edgePercent);

        finalSpeed = currentSpeed * dragMultiplier;
        horizontalOffset = Mathf.Clamp(horizontalOffset, -4f, 4f);

        Vector3 finalPosition = sample.position + sample.right * horizontalOffset;
        finalPosition.y = sample.position.y;
        transform.position = finalPosition;
    }
    #endregion

    #region Obstacle
    private void RanOverObstacle(ObstacleType obstacleType)
    {
        switch (obstacleType)
        {
            case ObstacleType.Oil:
                UpdateState(PlayerState.OilCrash);
                break;

            case ObstacleType.Mud:
                UpdateState(PlayerState.MudCrash);
                break;

            case ObstacleType.Barricade:
                UpdateState(PlayerState.BarricadeCrash);
                break;
        }
    }

    private IEnumerator OilCrash()
    {
        oilCrash = true;
        decelerationRate = currentSpeed * 0.9f;
        animationController.UpdateCycleAnimState(CycleAnimState.OilCrash);

        yield return new WaitForSeconds(2f);
        oilCrash = false;
        decelerationRate = actualDecelerationRate;

        UpdateState(PlayerState.Move);
    }
    
    private void ResetOilCrashRoutine()
    {
        if (oilCrashRoutine != null)
        {
            decelerationRate = actualDecelerationRate;
            StopCoroutine(oilCrashRoutine);
        }
    }

    private IEnumerator MudCrash()
    {
        maxSpeed = 15f;
        yield return new WaitForSeconds(5f);

        maxSpeed = actualMaxSpeed;
        UpdateState(PlayerState.Move);
    }

    private void ResetMudCrashRoutine()
    {
        if (mudCrashRoutine != null)
        {
            maxSpeed = actualMaxSpeed;
            StopCoroutine(mudCrashRoutine);
        }
    }

    private IEnumerator BarricadeCrash()
    {
        playerCanMove = false;
        currentSpeed = 0f;
        SplineSample sample = new();
        spline.Project(transform.position, ref sample);
        currentYaw = Quaternion.LookRotation(sample.forward).eulerAngles.y;
        animationController.UpdateCycleAnimState(CycleAnimState.Idle);

        yield return new WaitForSeconds(3.5f);

        UpdateState(PlayerState.Move);
    }

    private void ResetBarricadeCrashRoutine()
    {
        if (barricadeCrashRoutine != null)
        {
            StopCoroutine(barricadeCrashRoutine);
            UpdateState(PlayerState.Move);
        }
    }
    #endregion

    private bool IsMovingOppositeDirection()
    {
        SplineSample sample = new();

        spline.Project(transform.position, ref sample);

        float dot = Vector3.Dot(transform.forward, sample.forward);

        return dot < 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Oil"))
        {
            RanOverObstacle(ObstacleType.Oil);
        }
        else if (other.gameObject.CompareTag("Mud"))
        {
            RanOverObstacle(ObstacleType.Mud);
        }
        else if (other.gameObject.CompareTag("Barr") && other.gameObject.TryGetComponent(out Barricade comp))
        {
            if (comp.IsActive())
            {
                RanOverObstacle(ObstacleType.Barricade);
                comp.DestroyBarricade();
            }
        }
    }

    private void ResetAll()
    {
        if (oilCrashRoutine != null)
        {
            StopCoroutine(oilCrashRoutine);
        }
        if (mudCrashRoutine != null)
        {
            StopCoroutine(mudCrashRoutine);
        }
        if (barricadeCrashRoutine != null)
        {
            StopCoroutine(barricadeCrashRoutine);
        }

        currentSpeed = 0f;
        currentLean = 0f;
        targetLean = 0f;
        finalSpeed = 0f;
        decelerationRate = actualDecelerationRate;
        maxSpeed = actualMaxSpeed;
        oilCrash = false;
        playerCanMove = false;

        playerState = PlayerState.Idle;
        animationController.UpdateCycleAnimState(CycleAnimState.Idle);

        transform.SetPositionAndRotation(startPos, startRot);
        SplineSample sample = new();
        spline.Project(transform.position, ref sample);
        currentYaw = Quaternion.LookRotation(sample.forward).eulerAngles.y;
    }

    void OnDestroy()
    {
        inputManager.OnMoveContinues -= MovementManagement;
        GameManager.StartRace -= StartRace;
        GameManager.StopRace -= StopRace;
        GameManager.ResetAll -= ResetAll;
    }
}

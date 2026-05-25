using System.Collections;
using Dreamteck.Splines;
using K;
using K.DataType;
using UnityEngine;

public enum AIState
{
    Idle,
    Move,
    Won,
    Defeated,
    CrashedOilObstacle,
    CrashedBarricadeObstacle,
}

public enum AIObstacleIntelligence
{
    Dumb,
    Confused,
    Smart,
}

public class AIController : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds1 = new WaitForSeconds(1f);
    [SerializeField] private AnimationController animationController;
    [SerializeField] private SplineFollower splineFollower;
    [SerializeField] private AIConfig aIConfig;

    private AIState aiState;
    private float currentSpeed;
    private MinMax laneOffset = new(-3.5f, 3.5f);
    private LaneSide currentLane;
    private readonly float laneSwitchSpeed = 25f;
    private Coroutine laneSwitchRoutine = null;
    private Coroutine oilCrashRoutine = null;

    void Start()
    {
        currentLane = aIConfig.startLane;
        splineFollower.motion.offset = new(currentLane == LaneSide.Left ? laneOffset.min : laneOffset.max, 0);
        aiState = AIState.Idle;
    }

    void Update()
    {
        ContinuesUpdateState();
    }

    private void UpdateState(AIState _aiState)
    {
        if (aiState == _aiState)
        {
            return;
        }

        aiState = _aiState;

        switch (aiState)
        {
            case AIState.Idle:
                animationController.UpdateCycleAnimState(CycleAnimState.Idle);
                splineFollower.followSpeed = 0f;
                break;

            case AIState.Move:
                Move();
                break;

            case AIState.CrashedOilObstacle:
                if (oilCrashRoutine != null)
                    StopCoroutine(oilCrashRoutine);

                oilCrashRoutine = StartCoroutine(OilCrash());
                break;
        }
    }

    private void ContinuesUpdateState()
    {
        switch (aiState)
        {
            case AIState.Move:
                Move();
                break;
        }
    }

    private void Move()
    {
        float targetSpeed = aIConfig.maxSpeed;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            aIConfig.acceleration * Time.deltaTime
        );

        splineFollower.followSpeed = currentSpeed;

        animationController.UpdateCycleAnimState(CycleAnimState.Move, currentSpeed / aIConfig.maxSpeed);
    }

    public void CrashOnObstacle(ObstacleType obstacleType)
    {
        switch (obstacleType)
        {
            case ObstacleType.Oil:
                UpdateState(AIState.CrashedOilObstacle);
                break;

            case ObstacleType.Barricade:
                break;
        }
    }

    public void DetectedObstacle(ObstacleType obstacleType)
    {
        AIObstacleIntelligence intelligence = aIConfig.aIObstacleIntelligence;
        if (aIConfig.aIObstacleIntelligence == AIObstacleIntelligence.Confused)
        {
            intelligence = Random.value < 0.5f ? AIObstacleIntelligence.Dumb : AIObstacleIntelligence.Smart;
        }

        if (intelligence == AIObstacleIntelligence.Smart)
        {
            if (laneSwitchRoutine != null)
                StopCoroutine(laneSwitchRoutine);

            laneSwitchRoutine = StartCoroutine(SwitchLane());
        }
    }

    private IEnumerator SwitchLane()
    {
        Debug.Log("Switching");
        currentLane =
            currentLane == LaneSide.Left
                ? LaneSide.Right
                : LaneSide.Left;

        float targetLane =
            currentLane == LaneSide.Left
                ? laneOffset.min
                : laneOffset.max;

        while (Mathf.Abs(splineFollower.motion.offset.x - targetLane) > 0.01f)
        {
            Vector2 offset = splineFollower.motion.offset;

            offset.x = Mathf.MoveTowards(
                offset.x,
                targetLane,
                laneSwitchSpeed * Time.deltaTime
            );

            splineFollower.motion.offset = offset;

            yield return null;
        }

        Vector2 finalOffset = splineFollower.motion.offset;
        finalOffset.x = targetLane;
        splineFollower.motion.offset = finalOffset;
    }

    private IEnumerator OilCrash()
    {
        float deceleration = currentSpeed * 0.9f;
        animationController.UpdateCycleAnimState(CycleAnimState.OilCrash);
        while (currentSpeed > 0.01f)
        {
            float targetSpeed = 0;

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                deceleration * Time.deltaTime
            );

            splineFollower.followSpeed = currentSpeed;

            yield return null;
        }

        yield return _waitForSeconds1;

        UpdateState(AIState.Move);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Oil"))
        {
            CrashOnObstacle(ObstacleType.Oil);
        }
    }

    #region Test
    [Header("Testing")]
    [OnValueChange(nameof(TestUpdateState))][SerializeField] private AIState TestAIState;

    public void TestUpdateState() => UpdateState(TestAIState);
    #endregion
}

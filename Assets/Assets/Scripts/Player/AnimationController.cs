using K;
using UnityEngine;

public enum CycleAnimState
{
    Idle,
    Move,
    Celebration,
    OilCrash
}

public class AnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    private CycleAnimState cycleAnimState;

    private int idleAnimId;
    private int moveAnimId;
    private int celebrationAnimId;
    private int moveSpeedId;
    private int oilCrashId;

    private const float transitionDuration = 0.2f;

    private void Start()
    {
        InitializeAnimatorHashes();
    }

    private void InitializeAnimatorHashes()
    {
        idleAnimId = Animator.StringToHash("Idle");

        moveAnimId = Animator.StringToHash("Move");

        celebrationAnimId = Animator.StringToHash("Celebration");

        moveSpeedId = Animator.StringToHash("Move Speed");

        oilCrashId = Animator.StringToHash("OilCrash");
    }

    public void UpdateCycleAnimState(CycleAnimState newState, float moveSpeed = 0f)
    {
        if (cycleAnimState == newState)
        {
            UpdateMoveSpeed(moveSpeed);

            return;
        }

        cycleAnimState = newState;

        switch (cycleAnimState)
        {
            case CycleAnimState.Idle:
                PlayIdle();
                break;

            case CycleAnimState.Move:
                PlayMove(moveSpeed);
                break;

            case CycleAnimState.Celebration:
                PlayCelebration();
                break;

            case CycleAnimState.OilCrash:
                PlayOilCrash();
                break;
        }
    }

    private void PlayIdle()
    {
        animator.CrossFadeInFixedTime(idleAnimId, transitionDuration);
    }

    private void PlayMove(float moveSpeed)
    {
        animator.CrossFadeInFixedTime(moveAnimId, transitionDuration);

        UpdateMoveSpeed(moveSpeed);
    }

    private void PlayCelebration()
    {
        animator.CrossFadeInFixedTime(celebrationAnimId, transitionDuration);
    }

    private void PlayOilCrash()
    {
        animator.CrossFadeInFixedTime(oilCrashId, transitionDuration);
    }

    private void UpdateMoveSpeed(float moveSpeed)
    {
        if (cycleAnimState != CycleAnimState.Move)
            return;

        animator.SetFloat(moveSpeedId, moveSpeed);
    }

    #region Test
    [Header("Testing")]
    [OnValueChange(nameof(TestAnimFunc))]
    [SerializeField] private CycleAnimState testAnimState;

    [OnValueChange(nameof(TestMoveSpeedFunc))]
    [Range(0f, 1f)]
    [SerializeField] private float testMoveSpeed;
    
    private void TestAnimFunc()
    {
        UpdateCycleAnimState(testAnimState, testMoveSpeed);
    }

    private void TestMoveSpeedFunc()
    {
        UpdateCycleAnimState(testAnimState, testMoveSpeed);
    }
    #endregion
}
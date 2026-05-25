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
    [SerializeField] private Animator animator;

    private CycleAnimState cycleAnimState;

    private int idleAnimId = 0;
    private int moveAnimId = 0;
    private int celebrationAnimId = 0;
    private int moveSpeedIntId = 0;
    private int oilCrashId = 0;

    [Header("Testing")]
    [OnValueChange(nameof(TestAnimFunc))][SerializeField] private CycleAnimState TestAnimState;
    [OnValueChange(nameof(TestMoveSpeedFunc))][Range(0, 1)][SerializeField] private float TestMoveSpeed;

    void Start()
    {
        idleAnimId = Animator.StringToHash("Idle");
        moveAnimId = Animator.StringToHash("Move");
        celebrationAnimId = Animator.StringToHash("Celebration");
        moveSpeedIntId = Animator.StringToHash("Move Speed");
        oilCrashId = Animator.StringToHash("OilCrash");
    }

    public void UpdateCycleAnimState(CycleAnimState _cycleAnimState, float moveSpeed = 0)
    {
        if (cycleAnimState == _cycleAnimState)
        {
            if (cycleAnimState == CycleAnimState.Move)
            {
                animator.SetFloat(moveSpeedIntId, moveSpeed);
            }
            return;
        }        

        cycleAnimState = _cycleAnimState;

        switch (cycleAnimState)
        {
            case CycleAnimState.Idle:
                animator.CrossFadeInFixedTime(idleAnimId, 0.2f);
                break;

            case CycleAnimState.Move:
                animator.CrossFadeInFixedTime(moveAnimId, 0.2f);
                animator.SetFloat(moveSpeedIntId, moveSpeed);
                break;

            case CycleAnimState.Celebration:
                animator.CrossFadeInFixedTime(celebrationAnimId, 0.2f);
                break;

            case CycleAnimState.OilCrash:
                animator.CrossFadeInFixedTime(oilCrashId, 0.2f);
                break;
        }
    }

    public void TestAnimFunc() => UpdateCycleAnimState(TestAnimState, TestMoveSpeed);
    public void TestMoveSpeedFunc() => UpdateCycleAnimState(TestAnimState, TestMoveSpeed);
}

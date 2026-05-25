using UnityEngine;

public enum RacerID
{
    Player,
    AI1,
    AI2,
    AI3
}

public abstract class RacerControllerBase : MonoBehaviour
{
    [Header("Racer")]
    [SerializeField] private RacerID racerID;

    protected bool scoreBoosterActive;
    protected bool shieldActive;

    public RacerID RacerID => racerID;

    public bool ScoreBoosterActive
    {
        get => scoreBoosterActive;
    }

    public bool ShieldActive
    {
        get => shieldActive;
    }

    public bool IsPlayer => racerID == RacerID.Player;

    public bool IsAI => racerID != RacerID.Player;
}
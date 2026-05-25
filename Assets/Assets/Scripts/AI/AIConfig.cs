using UnityEngine;

[CreateAssetMenu(fileName = "AIConfig", menuName = "Scriptable Objects/AIConfig")]
public class AIConfig : ScriptableObject
{
    public float maxSpeed;
    public float acceleration;
    public AIObstacleIntelligence aIObstacleIntelligence;
    public LaneSide startLane;
}

using UnityEngine;

public class ObstacleDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AIController aiController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Oil"))
        {
            DetectObstacle(ObstacleType.Oil);

            return;
        }

        if (other.CompareTag("Mud"))
        {
            DetectObstacle(ObstacleType.Mud);

            return;
        }

        if (!other.CompareTag("Barr"))
            return;

        if (!other.TryGetComponent(out Barricade barricade))
            return;

        if (!barricade.IsActive())
            return;

        DetectObstacle(ObstacleType.Barricade);
    }

    private void DetectObstacle(ObstacleType obstacleType)
    {
        aiController.DetectedObstacle(obstacleType);
    }
}
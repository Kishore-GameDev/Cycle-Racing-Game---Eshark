using UnityEngine;

public class ObstacleDetector : MonoBehaviour
{
    [SerializeField] private AIController aIController;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Oil"))
        {
            aIController.DetectedObstacle(ObstacleType.Oil);
        }
        else if (other.gameObject.CompareTag("Mud"))
        {
            aIController.DetectedObstacle(ObstacleType.Mud);
        }
        else if (other.gameObject.CompareTag("Barr") && other.gameObject.TryGetComponent(out Barricade comp))
        {
            if (comp.IsActive())
            {
                aIController.DetectedObstacle(ObstacleType.Barricade);
            }
        }
    }
}

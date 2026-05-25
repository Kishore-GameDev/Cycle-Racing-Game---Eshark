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
    }
}

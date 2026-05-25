using UnityEngine;

public class ObstacleClearance : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Obstacle obstacle;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out RacerControllerBase racer))
            return;

        if (obstacle.DidCrash(racer))
            return;

        ScoreManager.AddObstacleClearScore(racer.RacerID, racer.ScoreBoosterActive);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float cacheDuration = 10f;

    private readonly HashSet<RacerControllerBase> crashedRacers = new();

    private void Start()
    {
        GameManager.ResetAll += ResetAll;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out RacerControllerBase racer))
            return;

        if (crashedRacers.Contains(racer) || racer.ShieldActive)
            return;

        crashedRacers.Add(racer);

        StartCoroutine(RemoveFromCache(racer));
    }

    public bool DidCrash(RacerControllerBase racer)
    {
        return crashedRacers.Contains(racer);
    }

    private IEnumerator RemoveFromCache(RacerControllerBase racer)
    {
        yield return new WaitForSeconds(cacheDuration);

        crashedRacers.Remove(racer);
    }

    private void ResetAll()
    {
        crashedRacers.Clear();

        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        GameManager.ResetAll -= ResetAll;
    }
}
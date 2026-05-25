using UnityEngine;

public class Barricade : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem vfx;
    [SerializeField] private GameObject visualObject;

    private bool alreadyDestroyed;

    private void Start()
    {
        SubscribeEvents();
    }

    private void SubscribeEvents()
    {
        GameManager.ResetAll += ResetAll;
    }

    private void UnsubscribeEvents()
    {
        GameManager.ResetAll -= ResetAll;
    }

    public bool IsActive()
    {
        return !alreadyDestroyed;
    }

    public void DestroyBarricade()
    {
        if (alreadyDestroyed)
            return;

        alreadyDestroyed = true;

        PlayDestroyEffects();

        SetActiveVisual(false);
    }

    private void PlayDestroyEffects()
    {
        vfx.Play();
    }

    private void SetActiveVisual(bool active)
    {
        visualObject.SetActive(active);
    }

    private void ResetAll()
    {
        alreadyDestroyed = false;

        vfx.Stop();

        SetActiveVisual(true);
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
}
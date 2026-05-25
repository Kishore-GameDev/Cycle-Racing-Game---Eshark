using UnityEngine;

public class Barricade : MonoBehaviour
{
    [SerializeField] private ParticleSystem VFX;
    [SerializeField] private GameObject visualObject;

    private bool alreadyDestroyed = false;

    void Start()
    {
        GameManager.ResetAll += ResetAll;
    }

    public bool IsActive() => !alreadyDestroyed;
    
    public void DestroyBarricade()
    {
        if (alreadyDestroyed) return;

        alreadyDestroyed = true;
        VFX.Play();
        visualObject.SetActive(false);
    }

    private void ResetAll()
    {
        alreadyDestroyed = false;
        VFX.Stop();
        visualObject.SetActive(true);
    }

    void OnDestroy()
    {
        GameManager.ResetAll -= ResetAll;
    }
}

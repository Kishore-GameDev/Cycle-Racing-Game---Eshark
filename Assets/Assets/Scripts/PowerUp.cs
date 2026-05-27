using System.Collections;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    private bool isActive = true;
    public bool IsActive => isActive;

    private Coroutine enableRoutine = null;

    void Start()
    {
        GameManager.ResetAll += Reset;
    }

    private void Reset()
    {
        if (enableRoutine != null)
        {
            StopCoroutine(enableRoutine);
        }

        SetActive(true);
    }

    private void SetActive(bool active)
    {
        isActive = active;
        transform.GetChild(0).gameObject.SetActive(active);
    }

    public void Triggered()
    {
        SetActive(false);
        enableRoutine = StartCoroutine(EnableAfterSeconds());
    }

    private IEnumerator EnableAfterSeconds()
    {
        yield return new WaitForSeconds(3f);
        SetActive(true);
    }

    void OnDestroy()
    {
        GameManager.ResetAll -= Reset;
    }
}

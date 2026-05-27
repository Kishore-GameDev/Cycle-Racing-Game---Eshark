using UnityEngine;
using UnityEngine.UI;

public class LapToggle : MonoBehaviour
{
    [SerializeField] private Image Lap1ButtonImage;
    [SerializeField] private Image Lap2ButtonImage;

    public void ButtonToggle(int lapCount)
    {
        SetAlpha(Lap1ButtonImage, lapCount == 1 ? 1f : 0f);
        SetAlpha(Lap2ButtonImage, lapCount == 2 ? 1f : 0f);

        GameManager.Instance.SetTotalLaps(lapCount);
    }

    private void SetAlpha(Image image, float alpha)
    {
        Color color = image.color;

        color.a = alpha;

        image.color = color;
    }
}

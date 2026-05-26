using TMPro;
using UnityEngine;

public class SingleRankScoreData : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void SetData(string name, string time, string score)
    {
        nameText.text = name;
        timeText.text = time;
        scoreText.text = score;
    }
}

[System.Serializable]
public struct SingleRankScoreDatas
{
    public string name;
    public string time;
    public string score;

    public SingleRankScoreDatas(string _name, string _time, string _score)
    {
        name = _name;
        time = _time;
        score = _score;
    }
}

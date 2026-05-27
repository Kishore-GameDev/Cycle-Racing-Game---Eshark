using Dreamteck.Splines;
using K;
using UnityEngine;

[ExecuteAlways]
public class TestSplineRotation : MonoBehaviour
{
    public SplineComputer spline;

    [OnValueChange(nameof(Rebuild))]
    public int pointIndex;

    public GameObject plane;

    private SplineSample sample = new();

    private void Update()
    {
        Rebuild();
    }

    private void Rebuild()
    {
        if (spline == null || plane == null)
            return;

        double percent =
            spline.GetPointPercent(pointIndex - 1);

        spline.Evaluate(percent, ref sample);

        Vector3 forward =
            sample.forward;

        if (forward == Vector3.zero)
            return;

        Quaternion rotation =
            Quaternion.LookRotation(
                forward,
                sample.up
            );

        plane.transform.SetPositionAndRotation(
            sample.position,
            rotation
        );
    }
}
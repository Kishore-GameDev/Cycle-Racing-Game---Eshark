using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Settings")]
    [SerializeField] private float positionSmooth = 8f;

    [SerializeField] private float rotationSmooth = 8f;

    private Vector3 localOffset;

    private void Start()
    {
        localOffset = target.InverseTransformPoint(transform.position);
    }

    private void LateUpdate()
    {
        Quaternion flatRotation =
            Quaternion.Euler(
                target.eulerAngles.x,
                target.eulerAngles.y,
                0f
            );

        Vector3 desiredPosition = target.TransformPoint(localOffset);

        
        transform.SetPositionAndRotation(
            Vector3.Lerp(
                            transform.position,
                            desiredPosition,
                            positionSmooth *
                            Time.deltaTime
                        ),
            Quaternion.Slerp(
                            transform.rotation,
                            flatRotation,
                            rotationSmooth *
                            Time.deltaTime
                        ));
    }
}
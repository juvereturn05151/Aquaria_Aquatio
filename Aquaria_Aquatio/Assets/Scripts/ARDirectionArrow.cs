// Used by scene: Assets/Scenes/Encounter_01_ARSearch.unity
using UnityEngine;

public class ARDirectionArrow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private Transform arrowTransform;
    [SerializeField] private Transform targetTransform;

    [Header("Presentation")]
    [SerializeField] private Vector3 arrowCameraOffset = new(0f, -0.35f, 0.85f);
    [SerializeField] private Vector3 arrowScale = new(0.25f, 0.25f, 0.55f);
    [SerializeField] private float arrowRotationSmoothSpeed = 8f;

    [Header("Debug Runtime")]
    [SerializeField] private Vector3 arrowTargetDirection;
    [SerializeField] private float distanceToTarget;
    [SerializeField] private float horizontalAngleToTarget;

    public Vector3 ArrowTargetDirection => arrowTargetDirection;
    public float DistanceToTarget => distanceToTarget;
    public float HorizontalAngleToTarget => horizontalAngleToTarget;

    public Transform TargetTransform
    {
        get => targetTransform;
        set => targetTransform = value;
    }

    public Camera ARCamera
    {
        get => arCamera;
        set => arCamera = value;
    }

    private void Reset()
    {
        arCamera = Camera.main;
        arrowTransform = transform;
    }

    private void Awake()
    {
        if (arrowTransform == null)
        {
            arrowTransform = transform;
        }

        arrowTransform.localScale = arrowScale;
    }

    private void LateUpdate()
    {
        if (arCamera == null || arrowTransform == null || targetTransform == null)
        {
            return;
        }

        UpdateArrow();
    }

    public void SetVisible(bool visible)
    {
        if (arrowTransform != null)
        {
            arrowTransform.gameObject.SetActive(visible);
        }
    }

    private void UpdateArrow()
    {
        arrowTransform.position = arCamera.transform.TransformPoint(arrowCameraOffset);
        arrowTransform.localScale = arrowScale;

        Vector3 directionToCreature = targetTransform.position - arCamera.transform.position;
        directionToCreature.y = 0f;
        distanceToTarget = directionToCreature.magnitude;

        if (directionToCreature.sqrMagnitude <= 0.001f)
        {
            return;
        }

        arrowTargetDirection = directionToCreature.normalized;
        Vector3 flatForward = Vector3.ProjectOnPlane(arCamera.transform.forward, Vector3.up);

        if (flatForward.sqrMagnitude > 0.001f)
        {
            horizontalAngleToTarget = Vector3.SignedAngle(
                flatForward.normalized,
                arrowTargetDirection,
                Vector3.up
            );
        }

        Quaternion targetRotation = Quaternion.LookRotation(arrowTargetDirection, Vector3.up);
        arrowTransform.rotation = Quaternion.Slerp(
            arrowTransform.rotation,
            targetRotation,
            Time.deltaTime * arrowRotationSmoothSpeed
        );
    }
}

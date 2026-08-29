// Used by scene: Assets/Scenes/Encounter_01_ARSearch.unity
using UnityEngine;

public class ARCreatureVisibilityDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private Transform creatureVisibilityTarget;

    [Header("Visibility")]
    [SerializeField] private float requiredVisibleTime = 0.75f;
    [SerializeField] private Vector2 viewportPadding = new(0.05f, 0.05f);

    [Header("Debug Runtime")]
    [SerializeField] private bool creatureInCameraView;
    [SerializeField] private float visibleTimer;
    [SerializeField] private Vector3 lastViewportPosition;

    public Camera ARCamera
    {
        get => arCamera;
        set => arCamera = value;
    }

    public Transform CreatureVisibilityTarget
    {
        get => creatureVisibilityTarget;
        set
        {
            creatureVisibilityTarget = value;
            ResetTimer();
        }
    }

    public bool CreatureInCameraView => creatureInCameraView;
    public float VisibleTimer => visibleTimer;
    public float RequiredVisibleTime => requiredVisibleTime;
    public Vector3 LastViewportPosition => lastViewportPosition;

    private void Reset()
    {
        arCamera = Camera.main;
    }

    public bool TickVisibility()
    {
        creatureInCameraView = IsTargetInCameraView();

        if (creatureInCameraView)
        {
            visibleTimer += Time.deltaTime;
        }
        else
        {
            visibleTimer = 0f;
        }

        return visibleTimer >= requiredVisibleTime;
    }

    public void ResetTimer()
    {
        visibleTimer = 0f;
        creatureInCameraView = false;
        lastViewportPosition = Vector3.zero;
    }

    private bool IsTargetInCameraView()
    {
        if (arCamera == null || creatureVisibilityTarget == null)
        {
            return false;
        }

        lastViewportPosition = arCamera.WorldToViewportPoint(creatureVisibilityTarget.position);
        return
            lastViewportPosition.z > 0f &&
            lastViewportPosition.x > viewportPadding.x &&
            lastViewportPosition.x < 1f - viewportPadding.x &&
            lastViewportPosition.y > viewportPadding.y &&
            lastViewportPosition.y < 1f - viewportPadding.y;
    }
}

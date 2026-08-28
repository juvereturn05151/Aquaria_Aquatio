using UnityEngine;

public class CreatureExplorationTarget : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private CreatureType creatureType;

    [Header("Position")]
    [SerializeField] private bool useDebugPosition = true;
    [SerializeField] private float debugEast;
    [SerializeField] private float debugNorth = 12f;
    [SerializeField] private float height = 0.6f;

    [Header("Debug Runtime")]
    [SerializeField] private Vector2 localEastNorthMeters;

    public CreatureType CreatureType => creatureType;
    public bool UseDebugPosition => useDebugPosition;
    public float DebugEast => debugEast;
    public float DebugNorth => debugNorth;
    public Vector2 LocalEastNorthMeters => localEastNorthMeters;
    public Vector3 LocalWorldPosition => transform.localPosition;

    private void Awake()
    {
        ApplyDebugPositionIfEnabled();
    }

    private void OnValidate()
    {
        ApplyDebugPositionIfEnabled();
    }

    public void ApplyDebugPositionIfEnabled()
    {
        if (useDebugPosition)
        {
            transform.localPosition = new Vector3(debugEast, height, debugNorth);
        }

        localEastNorthMeters = new Vector2(
            transform.localPosition.x,
            transform.localPosition.z
        );
    }
}

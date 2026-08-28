using UnityEngine;

public class CreatureExplorationTarget : MonoBehaviour
{
    [SerializeField] private CreatureType creatureType;
    [SerializeField] private float discoveryRadius = 15f;
    [SerializeField] private float encounterRadius = 3f;
    [SerializeField] private bool discovered;
    [SerializeField] private bool encounterStarted;

    public CreatureType CreatureType => creatureType;
    public float DiscoveryRadius => discoveryRadius;
    public float EncounterRadius => encounterRadius;
    public bool Discovered => discovered;
    public bool EncounterStarted => encounterStarted;
    public Vector3 LocalWorldPosition => transform.localPosition;

    public void MarkDiscovered()
    {
        discovered = true;
    }

    public bool TryStartEncounter()
    {
        if (encounterStarted)
        {
            return false;
        }

        encounterStarted = true;
        discovered = true;
        return true;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class CreatureSpawnManager : MonoBehaviour
{
    public event Action<CreatureExplorationTarget> OnCreatureEncounterReady;

    [SerializeField] private Transform targetsRoot;
    [SerializeField] private bool collectTargetsOnAwake = true;
    [SerializeField] private List<CreatureExplorationTarget> targets = new();

    public IReadOnlyList<CreatureExplorationTarget> Targets => targets;

    private void Awake()
    {
        if (collectTargetsOnAwake)
        {
            CollectTargets();
        }
    }

    [ContextMenu("Collect Creature Targets")]
    public void CollectTargets()
    {
        targets.Clear();

        Transform searchRoot = targetsRoot != null ? targetsRoot : transform;
        searchRoot.GetComponentsInChildren(true, targets);
    }

    public void NotifyEncounterReady(CreatureExplorationTarget target)
    {
        if (target == null)
        {
            return;
        }

        Debug.Log($"{target.CreatureType} Encounter Ready");
        OnCreatureEncounterReady?.Invoke(target);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public class CreatureSpawnManager : MonoBehaviour
{
    public event Action<CreatureExplorationTarget> OnCreatureEncounterReady;

    [SerializeField] private Transform targetsRoot;
    [SerializeField] private bool collectTargetsOnAwake = true;
    [SerializeField] private bool createMissingFlowTargets = true;
    [SerializeField] private List<CreatureExplorationTarget> targets = new();

    public IReadOnlyList<CreatureExplorationTarget> Targets => targets;

    private void Awake()
    {
        if (collectTargetsOnAwake)
        {
            CollectTargets();
        }

        if (createMissingFlowTargets)
        {
            EnsureFlowTargets();
        }
    }

    [ContextMenu("Collect Creature Targets")]
    public void CollectTargets()
    {
        targets.Clear();

        Transform searchRoot = targetsRoot != null ? targetsRoot : transform;
        searchRoot.GetComponentsInChildren(true, targets);
    }

    private void EnsureFlowTargets()
    {
        EnsureTarget(CreatureType.Aquaria, 0f, 12f);
        EnsureTarget(CreatureType.Aquario, 18f, 0f);
    }

    private void EnsureTarget(CreatureType creatureType, float debugEast, float debugNorth)
    {
        foreach (CreatureExplorationTarget target in targets)
        {
            if (target != null && target.CreatureType == creatureType)
            {
                return;
            }
        }

        Transform parent = targetsRoot != null ? targetsRoot : transform;
        GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        targetObject.name = $"{creatureType}_RuntimeTarget";
        targetObject.transform.SetParent(parent);
        targetObject.transform.localScale = Vector3.one * 1.2f;

        Material material = Resources.Load<Material>($"{creatureType}_Target");
        Renderer renderer = targetObject.GetComponent<Renderer>();

        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        Collider collider = targetObject.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        CreatureExplorationTarget createdTarget =
            targetObject.AddComponent<CreatureExplorationTarget>();
        createdTarget.Configure(creatureType, true, debugEast, debugNorth, 0.6f);
        targets.Add(createdTarget);
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

/*
CreatureSpawnManager.cs

Purpose:
Maintains the exploration creature target list, spawns targets near the player,
and emits encounter-ready notifications.

Responsibilities:
- Collect existing CreatureExplorationTarget components so scene-placed debug
  targets can be disabled at runtime.
- Spawn the current progression target around the player when exploration
  position is ready.
- Use assigned creature target prefabs when available, or generate primitive
  runtime targets as a fallback.
- Expose the active runtime target array used by CreatureProximitySystem.
- Invoke OnCreatureEncounterReady when proximity logic reports readiness.

Architecture:
Exploration target registry and spawn source. It creates active runtime targets,
while CreatureProximitySystem performs distance checks and reports encounter readiness.

Dependencies:
- CreatureExplorationTarget
- CreaturePresentation
- CreatureType
- Unity Resources materials for generated fallback targets

Events / Data Flow:
First valid player position
    -> SpawnTargetsNearPlayer()
    -> CreatureProximitySystem checks distances
    -> NotifyEncounterReady()
    -> OnCreatureEncounterReady event

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using System;
using System.Collections.Generic;
using UnityEngine;

public class CreatureSpawnManager : MonoBehaviour
{
    public event Action<CreatureExplorationTarget> OnCreatureEncounterReady;

    [SerializeField]
    private Transform targetsRoot;
    [SerializeField]
    private bool collectTargetsOnAwake = true;
    [SerializeField]
    private bool createMissingFlowTargets = true;
    [SerializeField]
    private bool spawnTargetsNearPlayerOnFirstPosition = true;
    [SerializeField]
    private CreatureExplorationTarget aquariaTargetPrefab;
    [SerializeField]
    private CreatureExplorationTarget aquarioTargetPrefab;
    [SerializeField]
    private float minimumSpawnDistance = 8f;
    [SerializeField]
    private float maximumSpawnDistance = 18f;
    [SerializeField]
    private float spawnHeight = 0.6f;
    [SerializeField]
    private List<CreatureExplorationTarget> targets = new();

    public IReadOnlyList<CreatureExplorationTarget> Targets => targets;
    public bool TargetsSpawnedNearPlayer { get; private set; }

    private void Awake()
    {
        if (collectTargetsOnAwake)
        {
            CollectTargets();
        }

        if (createMissingFlowTargets && !spawnTargetsNearPlayerOnFirstPosition)
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
        EnsureTarget(EncounterSessionData.CurrentSignalCreature);
    }

    private void EnsureTarget(CreatureType creatureType)
    {
        foreach (CreatureExplorationTarget target in targets)
        {
            if (target != null && target.CreatureType == creatureType)
            {
                return;
            }
        }

        CreatureExplorationTarget createdTarget = CreateTargetInstance(
            creatureType,
            GetPrefabForCreature(creatureType),
            Vector2.zero,
            null
        );
        targets.Add(createdTarget);
    }

    public void SpawnTargetsNearPlayer(
        Vector2 playerPosition,
        CreatureProximitySystem proximitySystem
    )
    {
        if (!spawnTargetsNearPlayerOnFirstPosition || TargetsSpawnedNearPlayer)
        {
            return;
        }

        DisableCollectedSceneTargets();
        targets.Clear();

        if (createMissingFlowTargets)
        {
            SpawnTargetNearPlayer(
                EncounterSessionData.CurrentSignalCreature,
                playerPosition,
                0,
                1,
                proximitySystem
            );
        }

        TargetsSpawnedNearPlayer = true;
    }

    private void SpawnTargetNearPlayer(
        CreatureType creatureType,
        Vector2 playerPosition,
        int targetIndex,
        int targetCount,
        CreatureProximitySystem proximitySystem
    )
    {
        float safeMinimumDistance = Mathf.Max(0f, minimumSpawnDistance);
        float safeMaximumDistance = Mathf.Max(safeMinimumDistance, maximumSpawnDistance);
        Vector2 offset = CreateSpawnOffset(
            targetIndex,
            targetCount,
            safeMinimumDistance,
            safeMaximumDistance
        );
        Vector2 targetPosition = playerPosition + offset;
        CreatureExplorationTarget spawnedTarget = CreateTargetInstance(
            creatureType,
            GetPrefabForCreature(creatureType),
            targetPosition,
            proximitySystem
        );
        targets.Add(spawnedTarget);
    }

    private CreatureExplorationTarget CreateTargetInstance(
        CreatureType creatureType,
        CreatureExplorationTarget targetPrefab,
        Vector2 targetPosition,
        CreatureProximitySystem proximitySystem
    )
    {
        Transform parent = targetsRoot != null ? targetsRoot : transform;
        CreatureExplorationTarget target;

        if (targetPrefab != null)
        {
            target = Instantiate(targetPrefab, parent);
            target.name = $"{creatureType}_RuntimeTarget";
        }
        else
        {
            GameObject targetObject = CreateGeneratedTargetObject(creatureType, parent);
            target = targetObject.AddComponent<CreatureExplorationTarget>();
        }

        target.Configure(creatureType, false, 0f, 0f, spawnHeight);
        target.SetRuntimePosition(targetPosition.x, targetPosition.y, spawnHeight);
        AssignProximitySystem(target, proximitySystem);
        return target;
    }

    private CreatureExplorationTarget GetPrefabForCreature(CreatureType creatureType)
    {
        return creatureType == CreatureType.Aquaria
            ? aquariaTargetPrefab
            : aquarioTargetPrefab;
    }

    private GameObject CreateGeneratedTargetObject(CreatureType creatureType, Transform parent)
    {
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

        return targetObject;
    }

    private void AssignProximitySystem(
        CreatureExplorationTarget target,
        CreatureProximitySystem proximitySystem
    )
    {
        if (proximitySystem == null)
        {
            return;
        }

        foreach (CreaturePresentation presentation in
            target.GetComponentsInChildren<CreaturePresentation>(true))
        {
            presentation.SetProximitySystem(proximitySystem);
        }
    }

    private void DisableCollectedSceneTargets()
    {
        foreach (CreatureExplorationTarget target in targets)
        {
            if (target != null)
            {
                target.gameObject.SetActive(false);
            }
        }
    }

    private Vector2 CreateSpawnOffset(
        int targetIndex,
        int targetCount,
        float minimumDistance,
        float maximumDistance
    )
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        if (targetCount > 1)
        {
            angle += targetIndex * Mathf.PI * 2f / targetCount;
        }

        float distance = UnityEngine.Random.Range(minimumDistance, maximumDistance);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
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

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
- ExplorationCreatureSignalPresentation
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
    public event Action OnTargetRelocated;

    [Header("Target Relocation")]
    [SerializeField, Min(0f)] private float minimumRelocationDistance = 15f;
    [SerializeField, Min(0f)] private float maximumRelocationDistance = 40f;
    [SerializeField, Min(1)] private int maximumRelocationAttempts = 10;
    [SerializeField, Min(0f)] private float relocationCooldown = 3f;

    private ExplorationPositionSourceSelector positionSourceSelector;
    private CreatureProximitySystem relocationProximitySystem;
    private ExplorationDebugPanel debugPanel;
    private float nextRelocationTime;
    private int lastRelocationFrame = -1;

    public void Initialize(ExplorationSystemInjector injector)
    {
        positionSourceSelector = injector.ExplorationPositionSourceSelector;
        relocationProximitySystem = injector.CreatureProximitySystem;
        debugPanel = injector.ExplorationDebugPanel;
    }

    public CreatureExplorationTarget CurrentTarget
    {
        get
        {
            foreach (CreatureExplorationTarget target in targets)
            {
                if (target != null && target.isActiveAndEnabled &&
                    target.CreatureType == EncounterSessionData.CurrentSignalCreature &&
                    EncounterSessionData.CanSearchFor(target.CreatureType))
                {
                    return target;
                }
            }
            return null;
        }
    }

    public bool CanRelocateCurrentTarget => isActiveAndEnabled &&
        positionSourceSelector != null &&
        positionSourceSelector.ActivePositionSource != null &&
        positionSourceSelector.ActivePositionSource.IsReady &&
        relocationProximitySystem != null && CurrentTarget != null &&
        Time.unscaledTime >= nextRelocationTime && Time.frameCount != lastRelocationFrame;

    public bool RelocateCurrentTarget()
    {
        if (!CanRelocateCurrentTarget)
        {
            return false;
        }

        // Guard the API itself, including failed rerolls and zero-second cooldowns.
        lastRelocationFrame = Time.frameCount;
        nextRelocationTime = Time.unscaledTime + Mathf.Max(0f, relocationCooldown);
        CreatureExplorationTarget target = CurrentTarget;
        ExplorationPositionSource source = positionSourceSelector.ActivePositionSource;
        Vector2 player = new Vector2(source.EastMeters, source.NorthMeters);
        Vector3 oldPosition = target.LocalWorldPosition;
        Vector2 oldEastNorth = new Vector2(oldPosition.x, oldPosition.z);
        float minimum = Mathf.Max(0f, minimumRelocationDistance);
        float maximum = Mathf.Max(minimum, maximumRelocationDistance);

        for (int attempt = 1; attempt <= Mathf.Max(1, maximumRelocationAttempts); attempt++)
        {
            // Same East/North offset convention as initial spawning. Keep the new
            // coordinate fixed under targetsRoot so later GPS movement cannot drag it.
            Vector2 candidate = player + CreateSpawnOffset(0, 1, minimum, maximum);
            float distance = Vector2.Distance(player, candidate);
            if (float.IsNaN(distance) || float.IsInfinity(distance) ||
                distance <= Mathf.Max(0f, relocationProximitySystem.EncounterRange) ||
                (candidate - oldEastNorth).sqrMagnitude < 0.01f)
            {
                continue;
            }

            target.SetRuntimePosition(candidate.x, candidate.y, oldPosition.y);
            relocationProximitySystem.RefreshAfterTargetRelocation();
            OnTargetRelocated?.Invoke();
            if (debugPanel != null && debugPanel.ShowDebug)
            {
                Debug.Log($"[Target Relocation] Target: {target.CreatureType} ({target.GetInstanceID()}) " +
                    $"Old East/Height/North: {oldPosition} New East/Height/North: {target.LocalWorldPosition} " +
                    $"Distance From Player: {distance:F1} m Attempts: {attempt}", target);
            }
            return true;
        }

        if (debugPanel != null && debugPanel.ShowDebug)
        {
            Debug.LogWarning("[Target Relocation] No candidate outside encounter range found; target unchanged.", this);
        }
        return false;
    }

    [Header("Spawning")]
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
        GameObject targetObject = new GameObject($"{creatureType}_RuntimeTarget");
        targetObject.name = $"{creatureType}_RuntimeTarget";
        targetObject.transform.SetParent(parent);

        GameObject signalVisual = new GameObject("SignalVisual");
        signalVisual.transform.SetParent(targetObject.transform);
        signalVisual.transform.localPosition = Vector3.zero;
        signalVisual.transform.localRotation = Quaternion.identity;
        signalVisual.transform.localScale = Vector3.one;

        GameObject signalRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        signalRing.name = "SignalRing";
        signalRing.transform.SetParent(signalVisual.transform);
        signalRing.transform.localPosition = Vector3.zero;
        signalRing.transform.localRotation = Quaternion.identity;
        signalRing.transform.localScale = new Vector3(1.8f, 0.03f, 1.8f);

        Material material = Resources.Load<Material>($"{creatureType}_Target");
        Renderer renderer = signalRing.GetComponent<Renderer>();

        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        Collider collider = signalRing.GetComponent<Collider>();

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
        if (target == null)
        {
            return;
        }

        ExplorationCreatureSignalPresentation[] presentations =
            target.GetComponentsInChildren<ExplorationCreatureSignalPresentation>(true);

        if (presentations.Length == 0)
        {
            presentations = new[]
            {
                target.gameObject.AddComponent<ExplorationCreatureSignalPresentation>()
            };
        }

        foreach (ExplorationCreatureSignalPresentation presentation in presentations)
        {
            if (presentation != null)
            {
                presentation.SetProximitySystem(proximitySystem);
            }
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

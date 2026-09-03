/*
EncounterCreatureLookAtPlayer.cs

Purpose:
Rotates an Encounter/AR creature so it faces the player horizontally.

Responsibilities:
- Cache a target Transform, usually the AR camera.
- Rotate this encounter creature toward that target.
- Ignore vertical height differences so the creature remains upright.

Intentionally not responsible for:
- AR spawning or plane placement.
- Exploration signal presentation.
- GPS/proximity/encounter detection.
- UI or scene transitions.

Intended use:
Attach to Encounter creature prefabs only, such as AquariaCreature_Encounter
and AquarioCreature_Encounter. Rotate the prefab root; use VisualRoot local
rotation to correct imported model forward-axis issues.

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using UnityEngine;

public class EncounterCreatureLookAtPlayer : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private bool instantRotation;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public float RotationSpeed
    {
        get => rotationSpeed;
        set => rotationSpeed = value;
    }

    public bool InstantRotation
    {
        get => instantRotation;
        set => instantRotation = value;
    }

    private void Update()
    {
        FaceTarget();
    }

    private void FaceTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        if (instantRotation)
        {
            transform.rotation = targetRotation;
            return;
        }

        float rotationStep = Mathf.Max(0f, rotationSpeed) * Time.deltaTime;
        transform.rotation = rotationStep > 0f
            ? Quaternion.Slerp(transform.rotation, targetRotation, rotationStep)
            : targetRotation;
    }
}

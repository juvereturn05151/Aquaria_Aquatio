/*
ARDirectionArrow.cs

Purpose:
Positions and rotates an in-world arrow so the player can turn toward the
spawned AR creature.

Responsibilities:
- Follow the AR camera at a configurable local offset.
- Point toward the assigned target on the horizontal plane.
- Hide or show the arrow object.
- Expose distance and bearing data for debug UI.

Architecture:
AR encounter presentation helper updated in LateUpdate after camera movement.

Dependencies:
- Camera
- Arrow Transform
- Target Transform assigned at runtime

Data Flow:
ARCreatureSearchController assigns a target and visibility state
    -> ARDirectionArrow updates world-space arrow orientation

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

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

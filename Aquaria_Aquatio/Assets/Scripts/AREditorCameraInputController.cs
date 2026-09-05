/*
AREditorCameraInputController.cs

Purpose:
Provides keyboard and mouse camera controls for testing the AR encounter scene
without walking around with a device.

Responsibilities:
- Move the camera root using WASD/QE input.
- Rotate the camera from mouse movement while the scene is active.
- Support sprint movement while Shift is held.
- Manage cursor lock state during look input.
- Stay isolated from encounter detection, UI, spawning, GPS, and scene flow.

Architecture:
Development/testing input helper for the AR encounter scene. It is separate
from AR tracking and only moves a regular Transform.

Dependencies:
- Camera or Camera.main
- UnityEngine.Input

Editor / Runtime:
Editor simulation by default. Player builds do not run it unless enableInPlayer
is explicitly enabled, and Android builds are always ignored.

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

using UnityEngine;

public class AREditorCameraInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform moveRoot;
    [SerializeField] private Transform yawRoot;
    [SerializeField] private Transform pitchRoot;

    [Header("Movement")]
    [SerializeField] private bool enableInEditor = true;
    [SerializeField] private bool enableInPlayer;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintMultiplier = 3f;
    [SerializeField] private float verticalMoveSpeed = 3f;

    [Header("Mouse Look")]
    [SerializeField] private bool requireRightMouseButton;
    [SerializeField] private float mouseSensitivity = 2.5f;
    [SerializeField] private float minimumPitch = -75f;
    [SerializeField] private float maximumPitch = 75f;

    [Header("Debug Runtime")]
    [SerializeField] private Vector3 currentMoveInput;
    [SerializeField] private float yaw;
    [SerializeField] private float pitch;

    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;
    private bool cursorStateCaptured;

    private void Reset()
    {
        moveRoot = transform;
        yawRoot = transform;
        Camera mainCamera = Camera.main;
        pitchRoot = mainCamera != null ? mainCamera.transform : transform;
    }

    private void Awake()
    {
        if (moveRoot == null)
        {
            moveRoot = transform;
        }

        if (yawRoot == null)
        {
            yawRoot = moveRoot;
        }

        if (pitchRoot == null)
        {
            pitchRoot = Camera.main != null ? Camera.main.transform : yawRoot;
        }

        Vector3 yawEuler = yawRoot.eulerAngles;
        Vector3 pitchEuler = pitchRoot.localEulerAngles;
        yaw = yawEuler.y;
        pitch = NormalizeAngle(pitchEuler.x);
    }

    private void Update()
    {
        if (!ShouldRun())
        {
            RestoreCursorIfNeeded();
            return;
        }

        UpdateMouseLook();
        UpdateMovement();
    }

    private bool ShouldRun()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return false;
#else
        return Application.isEditor ? enableInEditor : enableInPlayer;
#endif
    }

    private void UpdateMouseLook()
    {
        bool looking = !requireRightMouseButton || Input.GetMouseButton(1);

        if (!looking)
        {
            RestoreCursorIfNeeded();
            return;
        }

        CaptureCursorIfNeeded();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);

        if (yawRoot != null)
        {
            yawRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        if (pitchRoot != null)
        {
            pitchRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void UpdateMovement()
    {
        if (moveRoot == null)
        {
            return;
        }

        float x = GetAxis(KeyCode.A, KeyCode.D);
        float z = GetAxis(KeyCode.S, KeyCode.W);
        float y = GetAxis(KeyCode.Q, KeyCode.E);

        currentMoveInput = new Vector3(x, y, z);

        Vector3 forward = yawRoot != null ? yawRoot.forward : moveRoot.forward;
        Vector3 right = yawRoot != null ? yawRoot.right : moveRoot.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 horizontalMove = right * currentMoveInput.x + forward * currentMoveInput.z;

        if (horizontalMove.sqrMagnitude > 1f)
        {
            horizontalMove.Normalize();
        }

        float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            ? moveSpeed * sprintMultiplier
            : moveSpeed;
        Vector3 verticalMove = Vector3.up * currentMoveInput.y * verticalMoveSpeed;
        moveRoot.position += (horizontalMove * speed + verticalMove) * Time.deltaTime;
    }

    private static float GetAxis(KeyCode negative, KeyCode positive)
    {
        float axis = 0f;

        if (Input.GetKey(negative))
        {
            axis -= 1f;
        }

        if (Input.GetKey(positive))
        {
            axis += 1f;
        }

        return axis;
    }

    private void CaptureCursorIfNeeded()
    {
        if (cursorStateCaptured)
        {
            return;
        }

        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        cursorStateCaptured = true;
    }

    private void RestoreCursorIfNeeded()
    {
        if (!cursorStateCaptured)
        {
            return;
        }

        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;
        cursorStateCaptured = false;
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    private void OnDisable()
    {
        RestoreCursorIfNeeded();
    }
}

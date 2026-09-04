/*
SafeAreaRect.cs

Purpose:
Keeps a UI RectTransform inside the device safe area without creating UI at runtime.
*/

using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaRect : MonoBehaviour
{
    [SerializeField] private bool applySafeArea = true;

    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplyIfNeeded(true);
    }

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplyIfNeeded(true);
    }

    private void Update()
    {
        ApplyIfNeeded(false);
    }

    private void ApplyIfNeeded(bool force)
    {
        if (rectTransform == null)
        {
            return;
        }

        Rect safeArea = applySafeArea ? Screen.safeArea : new Rect(0f, 0f, Screen.width, Screen.height);
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

        if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
        {
            return;
        }

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}

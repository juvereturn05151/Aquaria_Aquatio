// Used by scene: Assets/Scenes/Exploration_04_EncounterEntry.unity
using UnityEngine;

public class EncounterEntryVirtualController : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] 
    private bool showController = true;
    [SerializeField] 
    private float buttonSize = 82f;
    [SerializeField] 
    private float buttonGap = 10f;
    [SerializeField] 
    private float edgePadding = 28f;

    [Header("Labels")]
    [SerializeField] 
    private string forwardLabel = "Forward";
    [SerializeField] 
    private string backLabel = "Back";
    [SerializeField] 
    private string leftLabel = "Left";
    [SerializeField] 
    private string rightLabel = "Right";
    [SerializeField] 
    private string rotateLeftLabel = "Rotate Left";
    [SerializeField] 
    private string rotateRightLabel = "Rotate Right";

    [Header("Debug Runtime")]
    [SerializeField] 
    private Vector2 moveInput;
    [SerializeField] 
    private float turnInput;

    [Header("References")]
    private EditorKeyboardPositionSource positionSource;
    private DeviceHeadingController headingController;

    private GUIStyle buttonStyle;

    public void Initialize(ExplorationSystemInjector explorationSystemInjector) 
    {
        positionSource = explorationSystemInjector.EditorKeyboardPositionSource;
        headingController = explorationSystemInjector.DeviceHeadingController;
    }

    private void Update()
    {
        if (positionSource != null)
        {
            positionSource.SetVirtualMoveInput(moveInput);
        }

        if (headingController != null)
        {
            headingController.SetVirtualTurnInput(turnInput);
        }
    }

    private void OnGUI()
    {
        if (!showController)
        {
            ClearInput();
            return;
        }

        EnsureStyle();

        moveInput = Vector2.zero;
        turnInput = 0f;

        float step = buttonSize + buttonGap;
        float moveOriginX = edgePadding;
        float moveOriginY = Screen.height - edgePadding - (buttonSize * 2f + buttonGap);

        if (HoldButton(new Rect(moveOriginX + step, moveOriginY, buttonSize, buttonSize), forwardLabel))
        {
            moveInput.y += 1f;
        }

        if (HoldButton(new Rect(moveOriginX + step, moveOriginY + step, buttonSize, buttonSize), backLabel))
        {
            moveInput.y -= 1f;
        }

        if (HoldButton(new Rect(moveOriginX, moveOriginY + step, buttonSize, buttonSize), leftLabel))
        {
            moveInput.x -= 1f;
        }

        if (HoldButton(new Rect(moveOriginX + step * 2f, moveOriginY + step, buttonSize, buttonSize), rightLabel))
        {
            moveInput.x += 1f;
        }

        float rotateOriginX = Screen.width - edgePadding - (buttonSize * 2f + buttonGap);
        float rotateOriginY = Screen.height - edgePadding - buttonSize;

        if (HoldButton(new Rect(rotateOriginX, rotateOriginY, buttonSize, buttonSize), rotateLeftLabel))
        {
            turnInput -= 1f;
        }

        if (HoldButton(new Rect(rotateOriginX + step, rotateOriginY, buttonSize, buttonSize), rotateRightLabel))
        {
            turnInput += 1f;
        }

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private bool HoldButton(Rect rect, string label)
    {
        return GUI.RepeatButton(rect, label, buttonStyle);
    }

    private void ClearInput()
    {
        moveInput = Vector2.zero;
        turnInput = 0f;
    }

    private void EnsureStyle()
    {
        if (buttonStyle != null)
        {
            return;
        }

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
        };
    }

    private void OnDisable()
    {
        ClearInput();

        if (positionSource != null)
        {
            positionSource.SetVirtualMoveInput(Vector2.zero);
        }

        if (headingController != null)
        {
            headingController.SetVirtualTurnInput(0f);
        }
    }
}

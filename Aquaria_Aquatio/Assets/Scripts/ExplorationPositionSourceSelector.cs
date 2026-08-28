using UnityEngine;

public class ExplorationPositionSourceSelector : MonoBehaviour
{
    [SerializeField] private bool useEditorSimulationInEditor = true;
    [SerializeField] private GPSPositionSource gpsPositionSource;
    [SerializeField] private EditorKeyboardPositionSource editorPositionSource;
    [SerializeField] private ExplorationController explorationController;
    [SerializeField] private CreatureProximitySystem proximitySystem;

    public ExplorationPositionSource ActivePositionSource { get; private set; }

    private void Awake()
    {
        SelectPositionSource();
    }

    private void Reset()
    {
        gpsPositionSource = FindAnyObjectByType<GPSPositionSource>();
        editorPositionSource = FindAnyObjectByType<EditorKeyboardPositionSource>();
        explorationController = FindAnyObjectByType<ExplorationController>();
        proximitySystem = FindAnyObjectByType<CreatureProximitySystem>();
    }

    private void SelectPositionSource()
    {
#if UNITY_EDITOR
        ActivePositionSource = useEditorSimulationInEditor && editorPositionSource != null
            ? editorPositionSource
            : gpsPositionSource;
#else
        ActivePositionSource = gpsPositionSource;
#endif

        if (gpsPositionSource != null)
        {
            gpsPositionSource.enabled = ActivePositionSource == gpsPositionSource;
        }

        if (editorPositionSource != null)
        {
            editorPositionSource.enabled = ActivePositionSource == editorPositionSource;
        }

        if (explorationController != null)
        {
            explorationController.SetPositionSource(ActivePositionSource);
        }

        if (proximitySystem != null)
        {
            proximitySystem.SetPositionSource(ActivePositionSource);
        }
    }
}

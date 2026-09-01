using UnityEngine;

public class ExplorationPositionSourceSelector : MonoBehaviour
{
    [SerializeField] 
    private bool useEditorSimulationInEditor = true;

    public ExplorationPositionSource ActivePositionSource { get; private set; }

    public void Initialize(ExplorationSystemInjector explorationSystemInjector)
    {
#if UNITY_EDITOR
        EditorKeyboardPositionSource editorPositionSource = explorationSystemInjector.EditorKeyboardPositionSource;
        GPSPositionSource gpsPositionSource = explorationSystemInjector.GPSPositionSource;
        ExplorationController explorationController = explorationSystemInjector.ExplorationController;
        CreatureProximitySystem proximitySystem = explorationSystemInjector.CreatureProximitySystem;

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

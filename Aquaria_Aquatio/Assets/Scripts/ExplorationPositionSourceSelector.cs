/*
ExplorationPositionSourceSelector.cs

Purpose:
Chooses the active exploration position source for the current scene.

Responsibilities:
- Receive GPS, editor simulation, movement, and proximity references from the injector.
- Select editor keyboard simulation in the Unity Editor when configured.
- Select GPSPositionSource outside the editor or when simulation is disabled.
- Enable the active source and disable the inactive source.
- Provide the active source to ExplorationController and CreatureProximitySystem.

Architecture:
Scene wiring component that keeps gameplay systems pointed at the selected
ExplorationPositionSource implementation.

Dependencies:
- ExplorationSystemInjector
- GPSPositionSource
- EditorKeyboardPositionSource
- ExplorationController
- CreatureProximitySystem

Data Flow:
Selected ExplorationPositionSource
    -> ExplorationController.SetPositionSource()
    -> CreatureProximitySystem.SetPositionSource()

Editor / Runtime:
Uses UNITY_EDITOR conditional selection to prefer editor simulation only inside
the Unity Editor when useEditorSimulationInEditor is true.

Copyright (c) 2026 Ju-ve Chankasemporn. All rights reserved.
*/

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

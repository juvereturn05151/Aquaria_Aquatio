using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Batch-only integration checks against the authored production scene. Does not
// save scenes or construct UI. Run with -executeMethod TargetRelocationValidation.Run.
public static class TargetRelocationValidation
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Run in a separate Unity batch process; this test changes in-memory progression.");

        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Production/Exploration_Production.unity");
            var injector = UnityEngine.Object.FindFirstObjectByType<ExplorationSystemInjector>();
            Require(injector != null, "Production injector exists");
            var manager = injector.CreatureSpawnManager;
            var proximity = injector.CreatureProximitySystem;
            var ui = injector.AquariaExplorationUI;
            EncounterSessionData.EnsureProgressionStarted(3);
            manager.Initialize(injector);
            proximity.Initialize(injector);
            injector.ExplorationPositionSourceSelector.Initialize(injector);
            var source = injector.ExplorationPositionSourceSelector.ActivePositionSource;
            SetPosition(source, 120f, -80f);
            ui.Initialize(injector);
            manager.CollectTargets();
            manager.SpawnTargetsNearPlayer(new Vector2(source.EastMeters, source.NorthMeters), proximity);
            var target = manager.CurrentTarget;
            Require(target != null, "Current target spawned");

            var button = new SerializedObject(ui).FindProperty("relocateTargetButton").objectReferenceValue as Button;
            Require(button != null && button.gameObject.activeInHierarchy, "Authored button reference resolves in production");
            Require(PrefabUtility.GetCorrespondingObjectFromSource(button) != null, "Button is inherited prefab content");
            Require(button.onClick.GetPersistentEventCount() == 1 &&
                button.onClick.GetPersistentTarget(0) == ui &&
                button.onClick.GetPersistentMethodName(0) == "PressRelocateTarget", "Serialized click targets HUD method");
            Require(button.transition == Selectable.Transition.ColorTint && button.targetGraphic is Image,
                "Button uses editable Image and native color states");
            Require(button.GetComponentInChildren<TextMeshProUGUI>().text == "Relocate Target", "Editable TMP label exists");
            Require(button.GetComponentsInChildren<Image>().Length == 2, "Background and icon are authored Images");
            Require(button.colors.disabledColor != button.colors.normalColor &&
                button.colors.pressedColor != button.colors.normalColor, "Disabled and pressed states are distinct");

            var identity = target.GetInstanceID();
            var type = target.CreatureType;
            target.SetRuntimePosition(source.EastMeters, source.NorthMeters, target.LocalWorldPosition.y);
            proximity.RefreshAfterTargetRelocation();
            Require(proximity.ProximityState == CreatureProximityState.EncounterReady, "Close target is encounter ready");
            var old = target.LocalWorldPosition;
            // Unity suppresses RuntimeOnly persistent listeners outside Play mode.
            // Enable this one in-memory for the batch edit-mode invocation only.
            button.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            button.onClick.Invoke();
            Require(target.LocalWorldPosition != old, "Authored click relocates target");
            Require(target.GetInstanceID() == identity && target.CreatureType == type && manager.Targets.Count == 1,
                "Same creature identity and single registry entry retained");
            Require(proximity.NearestCreatureDistance >= 15f && proximity.NearestCreatureDistance <= 40f,
                "Distance immediately refreshed into configured interval");
            Require(proximity.ProximityState != CreatureProximityState.EncounterReady && !button.interactable,
                "Readiness reset and button disabled immediately");
            var flow = new SerializedObject(ui).FindProperty("encounterFlow").objectReferenceValue as ExplorationEncounterFlow;
            Require(flow != null && !flow.EncounterReady, "Production encounter flow immediately refreshed");
            Require(!manager.RelocateCurrentTarget(), "Same-frame repeat rejected");
            Set(manager, "lastRelocationFrame", -1);
            Require(!manager.RelocateCurrentTarget(), "Cooldown rejects a later-frame request");

            var fixedPosition = target.LocalWorldPosition;
            SetPosition(source, 125f, -73f);
            proximity.RefreshAfterTargetRelocation();
            Require(target.LocalWorldPosition == fixedPosition, "Position updates do not drag the relocated target");
            float expected = Vector2.Distance(new Vector2(125f, -73f), new Vector2(fixedPosition.x, fixedPosition.z));
            Require(Mathf.Abs(proximity.NearestCreatureDistance - expected) < 0.001f, "Distance follows updated player position");

            ResetCooldown(manager);
            Require(manager.RelocateCurrentTarget(), "Relocation works after cooldown expires");
            Require(target.GetInstanceID() == identity && manager.Targets.Count == 1, "Repeated relocation creates no duplicate");
            Require(UnityEngine.Object.FindObjectsByType<CreatureExplorationTarget>(FindObjectsSortMode.None)
                .Count(t => t.isActiveAndEnabled) == 1, "Only one active exploration creature exists");

            ResetCooldown(manager);
            Set(manager, "minimumRelocationDistance", 0f);
            Set(manager, "maximumRelocationDistance", 0f);
            fixedPosition = target.LocalWorldPosition;
            Require(!manager.RelocateCurrentTarget() && target.LocalWorldPosition == fixedPosition,
                "Exhausted invalid candidates preserve current target");
            ResetCooldown(manager);
            target.gameObject.SetActive(false);
            Require(!manager.CanRelocateCurrentTarget && !manager.RelocateCurrentTarget(), "Inactive target rejected");
            target.gameObject.SetActive(true);
            typeof(ExplorationPositionSource).GetField("isReady", Fields).SetValue(source, false);
            Require(!manager.CanRelocateCurrentTarget, "Unavailable position disables relocation");
            Debug.Log("[Relocation Validation] PASS: production prefab wiring, identity, distance, readiness, cooldown, movement, failure handling.");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void SetPosition(ExplorationPositionSource source, float east, float north)
    {
        typeof(ExplorationPositionSource).GetMethod("UpdatePosition", Fields)
            .Invoke(source, new object[] { east, north, "Relocation integration test" });
    }

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, Fields).SetValue(target, value);

    private static void ResetCooldown(CreatureSpawnManager manager)
    {
        Set(manager, "nextRelocationTime", -1f);
        Set(manager, "lastRelocationFrame", -1);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log("[Relocation Validation] " + message);
    }
}

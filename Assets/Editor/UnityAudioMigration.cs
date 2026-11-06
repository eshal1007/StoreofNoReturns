using System;
using System.Collections.Generic;
using System.IO;
using BossFight.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-time scene migration from the legacy Wwise scene components to Unity's built-in AudioSource path.
/// It is safe to rerun from Tools/Audio whenever gameplay scenes change.
/// </summary>
public static class UnityAudioMigration
{
    private const string ShopScene = "Assets/Scenes/Scenes in Game/MainScene.unity";
    private const string ShopMusicPath = "Assets/Audio/Shop/shop_music_very_crunchy.wav";
    private const string ShopSfxDirectory = "Assets/Resources/ShopSfx";
    private static readonly string[] RequiredShopSfx =
    {
        StoreofNoReturnsSfx.CustomerExcited, StoreofNoReturnsSfx.CustomerDisappointed,
        StoreofNoReturnsSfx.CustomerArrive, StoreofNoReturnsSfx.CustomerEntryDoor,
        StoreofNoReturnsSfx.CustomerEntryChime, StoreofNoReturnsSfx.ReputationDown,
        StoreofNoReturnsSfx.ReputationUp, StoreofNoReturnsSfx.AccusationWrong,
        StoreofNoReturnsSfx.AccusationCorrect, StoreofNoReturnsSfx.UiConfirm,
        StoreofNoReturnsSfx.UiDeny, StoreofNoReturnsSfx.UiChoice, StoreofNoReturnsSfx.UiBack,
        StoreofNoReturnsSfx.UiAdvance, StoreofNoReturnsSfx.UiOpenDialogue,
        StoreofNoReturnsSfx.FlashlightOff, StoreofNoReturnsSfx.FlashlightOn,
        StoreofNoReturnsSfx.PlayerFootstep, StoreofNoReturnsSfx.ComputerInteract,
        StoreofNoReturnsSfx.GameOver
    };

    [MenuItem("Tools/Audio/Migrate Gameplay Scenes to Unity Audio")]
    public static void Run()
    {
        SoundBuilder.Build();

        int scenesChanged = 0;
        foreach (var buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled) continue;
            var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            bool changed = RemoveLegacyWwiseComponents(scene);
            changed |= EnsureSingleUnityListener(scene);
            if (buildScene.path == ShopScene) changed |= EnsureShopMusic(scene);
            if (!changed) continue;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            scenesChanged++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Unity audio migration complete. Updated {scenesChanged} gameplay scene(s).");
    }

    [MenuItem("Tools/Audio/Validate Unity Audio")]
    public static void Validate()
    {
        var errors = new List<string>();
        var sceneDependencies = new HashSet<string>(StringComparer.Ordinal);

        foreach (var buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled) continue;
            foreach (var dependency in AssetDatabase.GetDependencies(buildScene.path, true)) sceneDependencies.Add(dependency);

            var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            int enabledListeners = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
                {
                    if (listener.enabled && listener.gameObject.activeInHierarchy) enabledListeners++;
                }
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) continue;
                    var typeName = component.GetType().FullName;
                    if (typeName == "AkInitializer" || typeName == "AkAudioListener")
                    {
                        errors.Add($"{buildScene.path}: active legacy Wwise component {typeName}");
                    }
                    if (typeName == "AkGameObj" && component is Behaviour behaviour && behaviour.enabled)
                    {
                        errors.Add($"{buildScene.path}: enabled legacy Wwise component AkGameObj");
                    }
                }
            }
            if (enabledListeners != 1) errors.Add($"{buildScene.path}: expected one enabled Unity AudioListener, found {enabledListeners}");
        }

        foreach (var audioGuid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
        {
            var audioPath = AssetDatabase.GUIDToAssetPath(audioGuid);
            if (!sceneDependencies.Contains(audioPath)) errors.Add($"Unreferenced Unity audio clip: {audioPath}");
        }

        // Resources clips are loaded by StoreofNoReturnsSfx at runtime, so AssetDatabase cannot show them as scene
        // dependencies. Verify the complete recovered event set by its stable event names instead.
        foreach (var eventName in RequiredShopSfx)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{ShopSfxDirectory}/{eventName}.wav");
            if (clip == null) errors.Add($"Missing Unity shop sound effect for {eventName}");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Unity audio validation failed:\n- " + string.Join("\n- ", errors));
        }
        Debug.Log("Unity audio validation passed: scene clips, recovered Resources shop effects, and one Unity AudioListener per build scene are all present.");
    }

    private static bool RemoveLegacyWwiseComponents(Scene scene)
    {
        bool changed = false;
        foreach (var root in scene.GetRootGameObjects())
        {
            // Use Component rather than MonoBehaviour: the legacy listener/game-object components inherit through
            // Wwise's own base classes and are not always returned by a MonoBehaviour-only query after a reload.
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                // The integration's source files are in Wwise folders, but its three scene component classes live
                // in Unity's global namespace. Match their concrete names as well as namespaced Wwise components.
                var typeName = component.GetType().FullName;
                bool isLegacyWwiseComponent = typeName == "AkAudioListener"
                    || typeName == "AkGameObj"
                    || typeName == "AkInitializer"
                    || (!string.IsNullOrEmpty(typeName) && typeName.IndexOf("Wwise", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!isLegacyWwiseComponent) continue;

                // AkGameObj is re-added by the Wwise editor hook the first time a scene is saved after its listener
                // is removed. First disable it, then remove that inert component on the next migration pass.
                if (typeName == "AkGameObj" && component is Behaviour behaviour)
                {
                    if (behaviour.enabled) behaviour.enabled = false;
                    else UnityEngine.Object.DestroyImmediate(component);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(component);
                }
                changed = true;
            }
        }
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != "WwiseGlobal") continue;
            UnityEngine.Object.DestroyImmediate(root);
            changed = true;
        }
        return changed;
    }

    private static bool EnsureSingleUnityListener(Scene scene)
    {
        Camera selected = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                if (!camera.isActiveAndEnabled) continue;
                if (selected == null) selected = camera;
                if (camera.CompareTag("MainCamera"))
                {
                    selected = camera;
                    break;
                }
            }
            if (selected != null && selected.CompareTag("MainCamera")) break;
        }
        if (selected == null) return false;

        bool changed = false;
        var target = selected.GetComponent<AudioListener>();
        if (target == null)
        {
            target = selected.gameObject.AddComponent<AudioListener>();
            changed = true;
        }
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
            {
                bool shouldEnable = listener == target;
                if (listener.enabled == shouldEnable) continue;
                listener.enabled = shouldEnable;
                changed = true;
            }
        }
        return changed;
    }

    private static bool EnsureShopMusic(Scene scene)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ShopMusicPath);
        if (clip == null) throw new InvalidOperationException($"Missing Unity audio clip: {ShopMusicPath}");

        GameObject music = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "ShopMusic") music = root;
        }
        bool changed = false;
        if (music == null)
        {
            music = new GameObject("ShopMusic");
            SceneManager.MoveGameObjectToScene(music, scene);
            changed = true;
        }
        var source = music.GetComponent<AudioSource>();
        if (source == null)
        {
            source = music.AddComponent<AudioSource>();
            changed = true;
        }
        if (source.clip == clip && source.loop && source.playOnAwake && source.spatialBlend == 0f && Mathf.Approximately(source.volume, 0.5f)) return changed;

        source.clip = clip;
        source.loop = true;
        source.playOnAwake = true;
        source.spatialBlend = 0f;
        source.volume = 0.5f;
        return true;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps Unity's built-in audio path valid after every scene transition.  The project deliberately uses regular
/// AudioSource/AudioClip playback, which requires exactly one enabled AudioListener in each loaded scene.
/// </summary>
public static class UnityAudioListenerGuard
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureSingleListener(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSingleListener(scene);
    }

    private static void EnsureSingleListener(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

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
        if (selected == null) return;

        var selectedListener = selected.GetComponent<AudioListener>();
        if (selectedListener == null) selectedListener = selected.gameObject.AddComponent<AudioListener>();

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
            {
                listener.enabled = listener == selectedListener;
            }
        }
    }
}

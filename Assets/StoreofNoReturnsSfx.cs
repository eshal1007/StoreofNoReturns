using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shop sound-event bridge. The existing gameplay scripts retain their event names while this class loads the
/// recovered AudioClips from Resources and plays them with Unity AudioSources. No Wwise runtime or sound bank is used.
/// </summary>
public static class StoreofNoReturnsSfx
{
    private const string ResourceDirectory = "ShopSfx/";

    private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();
    private static readonly HashSet<string> MissingClipWarnings = new HashSet<string>();
    private static readonly HashSet<string> WorldEvents = new HashSet<string>
    {
        CustomerExcited, CustomerDisappointed, CustomerArrive, CustomerEntryDoor, CustomerEntryChime,
        FlashlightOff, FlashlightOn, PlayerFootstep, ComputerInteract
    };

    private static AudioSource uiSource;

    public const string CustomerExcited = "PLAY_CUSTOMER_EXCITED";
    public const string CustomerDisappointed = "PLAY_CUSTOMER_DISSAPOINTED";
    public const string CustomerArrive = "PLAY_CUSTOMER_ARRIVE";
    public const string CustomerEntryDoor = "PLAY_CUSTOMER_ENTRY_DOOR";
    public const string CustomerEntryChime = "PLAY_CUSTOMER_ENTRY_CHIME";
    public const string ReputationDown = "PLAY_REPUTATION_DOWN";
    public const string ReputationUp = "PLAY_REPUTATION_UP";
    public const string AccusationWrong = "PLAY_ACCUSATION_WRONG";
    public const string AccusationCorrect = "PLAY_ACCUSATION_CORRECT";
    public const string UiConfirm = "PLAY_UI_CONFIRM";
    public const string UiDeny = "PLAY_UI_DENY";
    public const string UiChoice = "PLAY_UI_CHOICE";
    public const string UiBack = "PLAY_UI_BACK";
    public const string UiAdvance = "PLAY_UI_ADVANCE";
    public const string UiOpenDialogue = "PLAY_UI_OPEN_DIALOGUE";
    public const string FlashlightOff = "PLAY_FLASHLIGHT_OFF";
    public const string FlashlightOn = "PLAY_FLASHLIGHT_ON";
    public const string PlayerFootstep = "PLAY_PLAYER_FOOTSTEP";
    public const string CustomerFootstep = PlayerFootstep;
    public const string ShopMusic = "PLAY_SHOP_MUSIC";
    public const string ComputerInteract = "PLAY_COMPUTER_INTERACT";
    public const string GameOver = "PLAY_GAME_OVER";

    public static void Post(string eventName, GameObject emitter)
    {
        // Shop music is an authored, looping AudioSource in MainScene.  All other former Wwise events are one-shots.
        if (string.IsNullOrEmpty(eventName) || eventName == ShopMusic) return;

        AudioClip clip = GetClip(eventName);
        if (clip == null)
        {
            if (MissingClipWarnings.Add(eventName))
            {
                Debug.LogWarning($"No Unity AudioClip found for shop sound event '{eventName}'.");
            }
            return;
        }

        if (emitter != null && WorldEvents.Contains(eventName))
        {
            AudioSource.PlayClipAtPoint(clip, emitter.transform.position);
            return;
        }

        GetUiSource().PlayOneShot(clip);
    }

    private static AudioClip GetClip(string eventName)
    {
        AudioClip clip;
        if (ClipCache.TryGetValue(eventName, out clip)) return clip;

        clip = Resources.Load<AudioClip>(ResourceDirectory + eventName);
        ClipCache[eventName] = clip;
        return clip;
    }

    private static AudioSource GetUiSource()
    {
        if (uiSource != null) return uiSource;

        var host = new GameObject("Shop UI Sound Effects");
        Object.DontDestroyOnLoad(host);
        uiSource = host.AddComponent<AudioSource>();
        uiSource.playOnAwake = false;
        uiSource.spatialBlend = 0f;
        return uiSource;
    }
}

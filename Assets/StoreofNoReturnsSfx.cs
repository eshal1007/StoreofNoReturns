using UnityEngine;

/// <summary>
/// Backwards-compatible event names used by the shop gameplay scripts. The project now uses Unity AudioSource and
/// AudioClip playback; the available shop clip is the looping music source in MainScene. The old event-only shop
/// effects have no source audio files in this project, so their existing call sites intentionally do nothing rather
/// than attempting to initialize or call Wwise at runtime.
/// </summary>
public static class StoreofNoReturnsSfx
{
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
        // Retained so existing gameplay scripts compile while source clips for these former Wwise-only events are
        // unavailable. Unity AudioSources handle every audio file that is present in Assets/Audio.
    }
}

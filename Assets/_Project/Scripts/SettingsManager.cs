using UnityEngine;

/// <summary>
/// Persistent store for game settings. Holds current values, applies them, and
/// saves/loads via PlayerPrefs so they survive scene loads and restarts.
///
/// Audio apply hooks are intentionally STUBBED for now (option B): the UI is
/// fully built and values are stored/persisted, but the actual Wwise routing for
/// Master / SFX needs dedicated RTPCs+buses authored in the Wwise project first.
/// Music volume already has a MusicVolume RTPC (wired via AudioManager) but is
/// left as a TODO here too, to keep all audio wiring in one place for later.
///
/// Display settings (resolution + window mode) ARE applied for real.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // ── Curated resolution options ─────────────────────────────────────────────
    public static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
    };

    public static string[] ResolutionLabels()
    {
        var labels = new string[Resolutions.Length];
        for (int i = 0; i < Resolutions.Length; i++)
            labels[i] = $"{Resolutions[i].x} x {Resolutions[i].y}";
        return labels;
    }

    // ── Current values (0–100 for volumes) ─────────────────────────────────────
    public float masterVolume = 100f;
    public bool  musicEnabled = true;
    public float musicVolume  = 100f;
    public float sfxVolume    = 100f;
    public int   resolutionIndex = 2;   // default 1920x1080
    public bool  fullscreen   = true;
    private bool hasSavedDisplayPrefs = false;

    // ── PlayerPrefs keys ───────────────────────────────────────────────────────
    const string K_Master  = "settings.masterVolume";
    const string K_MusicOn  = "settings.musicEnabled";
    const string K_MusicVol = "settings.musicVolume";
    const string K_Sfx      = "settings.sfxVolume";
    const string K_Res      = "settings.resolutionIndex";
    const string K_Win      = "settings.fullscreen";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
            new GameObject("SettingsManager").AddComponent<SettingsManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        ApplyAll();
    }

    private void Load()
    {
        masterVolume    = PlayerPrefs.GetFloat(K_Master,  100f);
        musicEnabled    = PlayerPrefs.GetInt(K_MusicOn,   1) == 1;
        musicVolume     = PlayerPrefs.GetFloat(K_MusicVol, 100f);
        sfxVolume       = PlayerPrefs.GetFloat(K_Sfx,      100f);
        resolutionIndex = PlayerPrefs.GetInt(K_Res,        2);
        fullscreen      = PlayerPrefs.GetInt(K_Win,        1) == 1;
        hasSavedDisplayPrefs = PlayerPrefs.HasKey(K_Res) || PlayerPrefs.HasKey(K_Win);
    }

    public void ApplyAll()
    {
        ApplyMasterVolume();
        ApplyMusicVolume();
        ApplySfxVolume();
        // Only re-apply display on startup if the player previously chose one. This
        // avoids forcing a resolution/fullscreen change on first launch (and the
        // focus-disrupting Screen call that goes with it).
        if (hasSavedDisplayPrefs) ApplyDisplay();
        // music on/off intentionally not auto-applied here to avoid double-posting
        // the BackgroundMusic event at boot; wire fully when audio routing is ready.
    }

    // ── Audio (stubbed apply — option B) ───────────────────────────────────────
    public void SetMasterVolume(float v)
    {
        masterVolume = v;
        PlayerPrefs.SetFloat(K_Master, v);
        ApplyMasterVolume();
    }
    private void ApplyMasterVolume()
    {
        // TODO(audio): needs a "MasterVolume" RTPC bound to the Master bus in Wwise.
        // AkUnitySoundEngine.SetRTPCValue("MasterVolume", masterVolume);
    }

    public void SetMusicEnabled(bool on)
    {
        musicEnabled = on;
        PlayerPrefs.SetInt(K_MusicOn, on ? 1 : 0);
        // TODO(audio): start/stop background music when wiring is finalized.
        // if (AudioManager.Instance != null) { if (on) AudioManager.Instance.StartMusic(); else AudioManager.Instance.StopMusic(); }
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = v;
        PlayerPrefs.SetFloat(K_MusicVol, v);
        ApplyMusicVolume();
    }
    private void ApplyMusicVolume()
    {
        // TODO(audio): connect to MusicVolume RTPC.
        // if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(musicVolume);
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = v;
        PlayerPrefs.SetFloat(K_Sfx, v);
        ApplySfxVolume();
    }
    private void ApplySfxVolume()
    {
        // TODO(audio): needs an "SfxVolume" RTPC bound to the SFX bus in Wwise.
        // AkUnitySoundEngine.SetRTPCValue("SfxVolume", sfxVolume);
    }

    // ── Display (applied for real) ─────────────────────────────────────────────
    public void SetResolutionIndex(int idx)
    {
        resolutionIndex = Mathf.Clamp(idx, 0, Resolutions.Length - 1);
        PlayerPrefs.SetInt(K_Res, resolutionIndex);
        ApplyDisplay();
    }

    public void SetFullscreen(bool fs)
    {
        fullscreen = fs;
        PlayerPrefs.SetInt(K_Win, fs ? 1 : 0);
        ApplyDisplay();
    }

    private void ApplyDisplay()
    {
        // Resolution/fullscreen changes are ignored by the editor Game view anyway,
        // and calling Screen.SetResolution there can steal keyboard focus from the
        // Game view (so movement input stops registering). Only apply in a build.
        if (Application.isEditor) return;

        FullScreenMode mode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (resolutionIndex >= 0 && resolutionIndex < Resolutions.Length)
        {
            Vector2Int r = Resolutions[resolutionIndex];
            Screen.SetResolution(r.x, r.y, mode);
        }
        else
        {
            Screen.fullScreenMode = mode;
        }
    }
}

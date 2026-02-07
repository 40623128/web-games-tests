using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("BGM")]
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float bgmVolume = 0.6f;

    [Header("SFX Master")]
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    public Vector2 sfxPitchRange = new Vector2(0.96f, 1.04f);

    [Header("SFX Clips")]
    public AudioClip sfxShoot;
    public AudioClip sfxAsteroidHitAsteroid;
    public AudioClip sfxAsteroidHitPlayer;
    public AudioClip sfxAsteroidHitByBullet;

    [Header("SFX Limiter")]
    public float minIntervalPerKey = 0.05f;   // 同一種音效最短間隔（防爆音）
    public float minIntervalGlobal = 0.01f;   // 全域最短間隔

    [Header("Upgrade SFX")]
    public AudioClip upgradeOpenClip;
    public AudioClip upgradeChooseClip;

    AudioSource bgmSrc;
    AudioSource sfxSrc;

    float _lastGlobalTime = -999f;
    readonly System.Collections.Generic.Dictionary<int, float> _lastKeyTime =
        new System.Collections.Generic.Dictionary<int, float>(64);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // BGM source
        bgmSrc = GetComponent<AudioSource>();
        if (bgmSrc == null) bgmSrc = gameObject.AddComponent<AudioSource>();
        bgmSrc.playOnAwake = false;
        bgmSrc.loop = true;
        bgmSrc.clip = bgmClip;
        bgmSrc.volume = bgmVolume;

        // SFX source (獨立)
        sfxSrc = gameObject.AddComponent<AudioSource>();
        sfxSrc.playOnAwake = false;
        sfxSrc.loop = false;
        sfxSrc.spatialBlend = 0f; // 2D
        sfxSrc.volume = sfxVolume;
    }

    void Start()
    {
        if (bgmClip != null && !bgmSrc.isPlaying) bgmSrc.Play();
    }

    // ---------------- BGM ----------------
    public void SetBGMVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        if (bgmSrc != null) bgmSrc.volume = bgmVolume;
    }

    public void StopBGM() { if (bgmSrc != null) bgmSrc.Stop(); }
    public void PlayBGM() { if (bgmSrc != null && !bgmSrc.isPlaying) bgmSrc.Play(); }

    // ---------------- SFX ----------------
    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        if (sfxSrc != null) sfxSrc.volume = sfxVolume;
    }

    void PlaySfx(AudioClip clip, int key, float volumeMul = 1f)
    {
        if (clip == null || sfxSrc == null) return;

        float now = Time.unscaledTime;

        // global limiter
        if (now - _lastGlobalTime < minIntervalGlobal) return;
        _lastGlobalTime = now;

        // per-key limiter
        if (_lastKeyTime.TryGetValue(key, out float t) && now - t < minIntervalPerKey) return;
        _lastKeyTime[key] = now;

        float oldPitch = sfxSrc.pitch;
        sfxSrc.pitch = Random.Range(sfxPitchRange.x, sfxPitchRange.y);
        sfxSrc.PlayOneShot(clip, sfxVolume * volumeMul);
        sfxSrc.pitch = oldPitch;
    }
    public void PlayUpgradeOpen()
    {
        if (sfxSrc == null || upgradeOpenClip == null) return;
        sfxSrc.PlayOneShot(upgradeOpenClip, sfxVolume);
    }

    public void PlayUpgradeChoose()
    {
        if (sfxSrc == null || upgradeChooseClip == null) return;
        sfxSrc.PlayOneShot(upgradeChooseClip, sfxVolume);
    }

    // 下面是你要用的 API（呼叫就播）
    public void PlayShoot() => PlaySfx(sfxShoot, 1);
    public void PlayAsteroidHitAsteroid() => PlaySfx(sfxAsteroidHitAsteroid, 2);
    public void PlayAsteroidHitPlayer() => PlaySfx(sfxAsteroidHitPlayer, 3);
    public void PlayAsteroidHitByBullet() => PlaySfx(sfxAsteroidHitByBullet, 4);


}

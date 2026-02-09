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
    public AudioClip sfxPlayerDeath;

    // ✅ 每個音效各自音量（倍率）
    [Header("Per-SFX Volume (0~1, multiplied by SFX Master)")]
    [Range(0f, 1f)] public float volShoot = 1.0f;
    [Range(0f, 1f)] public float volAsteroidHitAsteroid = 1.0f;
    [Range(0f, 1f)] public float volAsteroidHitPlayer = 1.0f;
    [Range(0f, 1f)] public float volAsteroidHitByBullet = 1.0f;
    [Range(0f, 1f)] public float volPlayerDeath = 1.0f;

    [Header("SFX Limiter")]
    public float minIntervalPerKey = 0.05f;
    public float minIntervalGlobal = 0.01f;

    [Header("Upgrade SFX")]
    public AudioClip upgradeOpenClip;
    public AudioClip upgradeChooseClip;

    // ✅ Upgrade 也給各自音量（可要可不要，但你說每個都要，就一起加）
    [Header("Upgrade Volume (0~1)")]
    [Range(0f, 1f)] public float volUpgradeOpen = 1.0f;
    [Range(0f, 1f)] public float volUpgradeChoose = 1.0f;

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

        bgmSrc = GetComponent<AudioSource>();
        if (bgmSrc == null) bgmSrc = gameObject.AddComponent<AudioSource>();
        bgmSrc.playOnAwake = false;
        bgmSrc.loop = true;
        bgmSrc.clip = bgmClip;
        bgmSrc.volume = bgmVolume;

        sfxSrc = gameObject.AddComponent<AudioSource>();
        sfxSrc.playOnAwake = false;
        sfxSrc.loop = false;
        sfxSrc.spatialBlend = 0f;
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

    void PlaySfx(AudioClip clip, int key, float perSfxMul = 1f)
    {
        if (clip == null || sfxSrc == null) return;

        float now = Time.unscaledTime;

        if (now - _lastGlobalTime < minIntervalGlobal) return;
        _lastGlobalTime = now;

        if (_lastKeyTime.TryGetValue(key, out float t) && now - t < minIntervalPerKey) return;
        _lastKeyTime[key] = now;

        float oldPitch = sfxSrc.pitch;
        sfxSrc.pitch = Random.Range(sfxPitchRange.x, sfxPitchRange.y);

        // ✅ 這裡就是：Master * 每個音效倍率
        sfxSrc.PlayOneShot(clip, sfxVolume * Mathf.Clamp01(perSfxMul));

        sfxSrc.pitch = oldPitch;
    }

    public void PlayUpgradeOpen()
    {
        if (sfxSrc == null || upgradeOpenClip == null) return;
        sfxSrc.PlayOneShot(upgradeOpenClip, sfxVolume * Mathf.Clamp01(volUpgradeOpen));
    }

    public void PlayUpgradeChoose()
    {
        if (sfxSrc == null || upgradeChooseClip == null) return;
        sfxSrc.PlayOneShot(upgradeChooseClip, sfxVolume * Mathf.Clamp01(volUpgradeChoose));
    }

    // ✅ 各自帶自己的倍率
    public void PlayShoot() => PlaySfx(sfxShoot, 1, volShoot);
    public void PlayAsteroidHitAsteroid() => PlaySfx(sfxAsteroidHitAsteroid, 2, volAsteroidHitAsteroid);
    public void PlayAsteroidHitPlayer() => PlaySfx(sfxAsteroidHitPlayer, 3, volAsteroidHitPlayer);
    public void PlayAsteroidHitByBullet() => PlaySfx(sfxAsteroidHitByBullet, 4, volAsteroidHitByBullet);
    public void PlayPlayerDeath() => PlaySfx(sfxPlayerDeath, 5, volPlayerDeath);
}

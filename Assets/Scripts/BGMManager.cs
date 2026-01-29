using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [Header("BGM")]
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float volume = 0.6f;

    private AudioSource src;

    void Awake()
    {
        // ✅ 防止重複生成（重開場景時不疊音）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);

        src = GetComponent<AudioSource>();
        if (src == null) src = gameObject.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.loop = true;
        src.clip = bgmClip;
        src.volume = volume;
    }

    void Start()
    {
        if (bgmClip != null && !src.isPlaying)
            src.Play();
    }

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (src != null) src.volume = volume;
    }

    public void StopBGM()
    {
        if (src != null) src.Stop();
    }

    public void PlayBGM()
    {
        if (src != null && !src.isPlaying) src.Play();
    }
}

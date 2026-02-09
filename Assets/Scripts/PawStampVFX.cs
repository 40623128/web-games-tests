using UnityEngine;

public class PawStampVFX : MonoBehaviour
{
    [Header("Life")]
    public float lifetime = 0.35f;

    [Header("Scale")]
    public float startScale = 0.6f;
    public float endScale = 1.15f;

    [Header("Fade")]
    public float startAlpha = 1.0f;
    public float endAlpha = 0.0f;

    [Header("Rotation")]
    public float randomRotDeg = 25f;

    SpriteRenderer sr;
    float t0;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        t0 = Time.time;

        // 隨機旋轉一點，較自然
        float z = Random.Range(-randomRotDeg, randomRotDeg);
        transform.rotation = Quaternion.Euler(0, 0, z);

        // 初始大小
        transform.localScale = Vector3.one * startScale;

        // 初始透明度
        if (sr != null)
        {
            var c = sr.color;
            c.a = startAlpha;
            sr.color = c;
        }
    }

    void Update()
    {
        float t = (Time.time - t0) / Mathf.Max(0.0001f, lifetime);
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // ease-out（先快後慢）
        float e = 1f - Mathf.Pow(1f - t, 3f);

        // 放大
        float s = Mathf.Lerp(startScale, endScale, e);
        transform.localScale = Vector3.one * s;

        // 淡出
        if (sr != null)
        {
            var c = sr.color;
            c.a = Mathf.Lerp(startAlpha, endAlpha, e);
            sr.color = c;
        }
    }
}

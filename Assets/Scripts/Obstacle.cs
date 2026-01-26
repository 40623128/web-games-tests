using System;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Scale")]
    public float minScale = 0.25f;
    public float maxScale = 2.0f;

    [Header("Aspect Ratio (X/Y)")]
    public float minAspect = 0.5f;  // 0.5 => 偏扁(寬<高)
    public float maxAspect = 2.0f;  // 2.0 => 偏長(寬>高)

    [Header("Speed")]
    public float minSpeed = 50.0f;
    public float maxSpeed = 50.0f;

    [Header("Drop")]
    public GameObject goldOrePrefab;
    public int goldDropCount = 1;
    public float dropSpread = 0.2f;

    [Header("New Spawn Speed vs Game Time")]
    public float growthRate = 0.08f;
    public float maxMultiplier = 5.0f;

    [Header("Effects")]
    public GameObject bounceEffectPrefab;
    public GameObject destroyEffectPrefab;
    public float effectLife = 1f;

    [Header("Spin")]
    public float maxSpinSpeed = 10f;

    private Rigidbody2D rb;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // 保留 prefab 原本比例
        Vector3 baseScale = transform.localScale;

        // 抽整體倍率 + 長寬比
        float s = UnityEngine.Random.Range(minScale, maxScale);
        float aspect = UnityEngine.Random.Range(minAspect, maxAspect); // X/Y

        float a = Mathf.Sqrt(Mathf.Max(0.0001f, aspect));
        float sx = s * a;
        float sy = s / a;

        // 套用：baseScale * (sx, sy)
        transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);

        // 質量：用面積 sqrt(x*y)（越大越重）
        float randomMass = Mathf.Sqrt(Mathf.Abs(transform.localScale.x * transform.localScale.y));
        randomMass = Mathf.Max(0.0001f, randomMass);

        float t = Time.timeSinceLevelLoad;
        float multiplier = Mathf.Min(1f + t * growthRate, maxMultiplier);

        float randomSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed) / randomMass;
        randomSpeed *= multiplier;

        Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;
        if (randomDirection.sqrMagnitude < 0.001f) randomDirection = Vector2.right;

        rb.AddForce(randomDirection * randomSpeed);

        float randomTorque = UnityEngine.Random.Range(-maxSpinSpeed, maxSpinSpeed);
        rb.AddTorque(randomTorque);
    }

    public void HitByBullet(Vector2 hitPoint)
    {
        if (isDead) return;
        isDead = true;

        PlayDestroyEffect(hitPoint);
        DropGold();
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.collider.CompareTag("Bullet")) return;

        if (bounceEffectPrefab != null)
        {
            Vector2 contactPoint = collision.GetContact(0).point;
            GameObject fx = Instantiate(bounceEffectPrefab, contactPoint, Quaternion.identity);
            Destroy(fx, effectLife);
        }
    }

    void PlayDestroyEffect(Vector2 pos)
    {
        if (destroyEffectPrefab == null) return;

        GameObject fx = Instantiate(destroyEffectPrefab, pos, Quaternion.identity);
        Destroy(fx, effectLife);
    }

    void DropGold()
    {
        if (goldOrePrefab == null) return;

        for (int i = 0; i < goldDropCount; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * dropSpread;
            Instantiate(goldOrePrefab, (Vector2)transform.position + offset, Quaternion.identity);
        }
    }
}

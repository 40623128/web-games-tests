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

    [Header("SFX")]
    public float collisionSfxCooldown = 0.08f; // 同顆隕石碰撞音最短間隔
    public float minImpactToPlay = 0.6f;       // 低於這個撞擊強度不播（避免輕微抖動一直播）
    float _lastCollisionSfxTime = -999f;


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
        BGMManager.Instance?.PlayAsteroidHitByBullet();
        PlayDestroyEffect(hitPoint);
        DropGold();
        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.collider.CompareTag("Bullet")) return;

        // ===== SFX：冷卻 + 強度判斷 =====
        float now = Time.unscaledTime;
        if (now - _lastCollisionSfxTime >= collisionSfxCooldown)
        {
            // 2D 碰撞強度：relativeVelocity 大小很常用
            float impact = collision.relativeVelocity.magnitude;

            if (impact >= minImpactToPlay)
            {
                if (collision.collider.CompareTag("Player"))
                {
                    BGMManager.Instance?.PlayAsteroidHitPlayer();     // ✅ 隕石撞玩家
                    _lastCollisionSfxTime = now;
                }
                else if (collision.collider.CompareTag("Obstacle"))
                {
                    BGMManager.Instance?.PlayAsteroidHitAsteroid();   // ✅ 隕石撞隕石
                    _lastCollisionSfxTime = now;
                }
                else
                {
                    // 其他（牆、Border 等）你要不要播碰撞音都行
                    // 例如：BGMManager.Instance?.PlayAsteroidHitAsteroid();
                    // _lastCollisionSfxTime = now;
                }
            }
        }

        // ===== 你的 bounce effect =====
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

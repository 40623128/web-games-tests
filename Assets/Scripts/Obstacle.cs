using System;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float minSizeX = 0.25f;
    public float maxSizeX = 2.0f;
    public float minSizeY = 0.25f;
    public float maxSizeY = 2.0f;

    public float minSpeed = 50.0f;
    public float maxSpeed = 50.0f;

    [Header("Drop")]
    public GameObject goldOrePrefab;   // 金礦 Prefab
    public int goldDropCount = 1;      // 一顆隕石掉幾個
    public float dropSpread = 0.2f;    // 掉落散開範圍

    [Header("New Spawn Speed vs Game Time")]
    public float growthRate = 0.08f;     // 每秒 +8%（只影響新生成隕石）
    public float maxMultiplier = 5.0f;   // 最多 3 倍

    [Header("Effects")]
    public GameObject bounceEffectPrefab;
    public GameObject destroyEffectPrefab;
    public float effectLife = 1f;

    [Header("Spin")]
    public float maxSpinSpeed = 10f;

    [Header("Hit Rules")]
    public bool destroyBulletOnHit = true;

    private Rigidbody2D rb;

    void Start()
    {
        float randomSizeX = UnityEngine.Random.Range(minSizeX, maxSizeX);
        float randomSizeY = UnityEngine.Random.Range(minSizeY, maxSizeY);
        transform.localScale = new Vector3(randomSizeX, randomSizeY, 1);

        float randomMass = (float)Math.Pow(randomSizeX * randomSizeY, 0.5f);

        rb = GetComponent<Rigidbody2D>();

        // ✅ 用遊戲經過時間決定這顆「出生速度倍率」
        float t = Time.timeSinceLevelLoad;
        float multiplier = 1f + t * growthRate;         // 線性成長
        multiplier = Mathf.Min(multiplier, maxMultiplier);

        float randomSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed) / randomMass;
        randomSpeed *= multiplier;                      // ✅ 只影響新生成的隕石

        Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;
        if (randomDirection.sqrMagnitude < 0.001f) randomDirection = Vector2.right;

        rb.AddForce(randomDirection * randomSpeed);

        float randomTorque = UnityEngine.Random.Range(-maxSpinSpeed, maxSpinSpeed);
        rb.AddTorque(randomTorque);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Bullet")) return;

        Vector2 hitPoint = other.ClosestPoint(transform.position);
        PlayDestroyEffect(hitPoint);

        if (destroyBulletOnHit) Destroy(other.gameObject);

        DropGold();              // ✅ 掉金礦
        Destroy(gameObject);      // ✅ 隕石消失
    }


    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Bullet"))
        {
            Vector2 contactPoint = collision.GetContact(0).point;
            PlayDestroyEffect(contactPoint);

            if (destroyBulletOnHit) Destroy(collision.gameObject);
            DropGold();              // ✅ 掉金礦
            Destroy(gameObject);      // ✅ 隕石消失
            return;
        }

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

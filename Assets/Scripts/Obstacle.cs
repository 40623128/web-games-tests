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
    private bool isDead = false; // ✅ 防止同一顆被打多次觸發兩次掉落/特效

    void Start()
    {
        float randomSizeX = UnityEngine.Random.Range(minSizeX, maxSizeX);
        float randomSizeY = UnityEngine.Random.Range(minSizeY, maxSizeY);
        transform.localScale = new Vector3(randomSizeX, randomSizeY, 1);

        float randomMass = (float)Math.Pow(randomSizeX * randomSizeY, 0.5f);
        rb = GetComponent<Rigidbody2D>();

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

    // ✅ 讓 Bullet 呼叫：隕石被打中

    public void HitByBullet(Vector2 hitPoint)
    {
        if (isDead) return;
        isDead = true;

        PlayDestroyEffect(hitPoint);
        DropGold();
        Destroy(gameObject);
    }

    // ✅ 撞牆/撞其他物件時的彈跳特效（不處理 Bullet）
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        // Bullet 不在這裡處理，避免與 Bullet 穿透邏輯打架
        if (collision.collider.CompareTag("Bullet")) return;

        if (bounceEffectPrefab != null)
        {
            Vector2 contactPoint = collision.GetContact(0).point;
            GameObject fx = Instantiate(bounceEffectPrefab, contactPoint, Quaternion.identity);
            Destroy(fx, effectLife);
        }
    }

    void Die(Vector2 hitPoint)
    {
        PlayDestroyEffect(hitPoint);
        DropGold();

        Destroy(gameObject);
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

using System;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float minSizeX = 0.25f;
    public float maxSizeX = 2.0f;
    public float minSizeY = 0.25f;
    public float maxSizeY = 2.0f;

    public float minSpeed = 50.0f;
    public float maxSpeed = 150.0f;

    [Header("Effects")]
    public GameObject bounceEffectPrefab;   // 撞牆/撞物 的特效（可留可不留）
    public GameObject destroyEffectPrefab;  // ✅ 隕石消失特效（爆炸）
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

        float randomSpeed = UnityEngine.Random.Range(minSpeed, maxSpeed) / randomMass;
        Vector2 randomDirection = UnityEngine.Random.insideUnitCircle.normalized;

        rb.AddForce(randomDirection * randomSpeed);

        float randomTorque = UnityEngine.Random.Range(-maxSpinSpeed, maxSpinSpeed);
        rb.AddTorque(randomTorque);
    }

    // ✅ 子彈（Trigger）打到：播放 destroy 特效 + 隕石消失
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Bullet")) return;

        // 取得碰撞點：用子彈 collider 在隕石上的最近點
        Vector2 hitPoint = other.ClosestPoint(transform.position);

        PlayDestroyEffect(hitPoint);

        if (destroyBulletOnHit) Destroy(other.gameObject);

        Destroy(gameObject);
    }

    // 可選：撞到其他物件時播放 bounce 特效
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 如果子彈不是 Trigger 而是 Collision，也可兼容
        if (collision.collider.CompareTag("Bullet"))
        {
            Vector2 contactPoint = collision.GetContact(0).point;
            PlayDestroyEffect(contactPoint);

            if (destroyBulletOnHit) Destroy(collision.gameObject);
            Destroy(gameObject);
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
}

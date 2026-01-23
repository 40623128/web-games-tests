using UnityEngine;

public class GoldPickupMagnet : MonoBehaviour
{
    public int value = 1;

    [Header("Magnet")]
    public float magnetRange = 3.0f;   // 秈硂禯瞒秨﹍
    public float pickupRange = 0.4f;   // 秈硂禯瞒碞衡具
    public float magnetSpeed = 8.0f;   // 硉
    public float lifeTime = 15f;

    private Transform player;

    void Start()
    {
        Destroy(gameObject, lifeTime);

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        else Debug.LogWarning("Gold: тぃ Tag=Player ン");
    }

    void Update()
    {
        if (player == null) return;

        float d = Vector2.Distance(transform.position, player.position);

        // 合
        if (d <= magnetRange)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                magnetSpeed * Time.deltaTime
            );
        }

        // 禯瞒具ぃ綼 Trigger具
        if (d <= pickupRange)
        {
            Pickup(player.gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Trigger 具狦Τ Trigger 砞﹚タ絋硂穦具
        if (other.CompareTag("Player"))
        {
            Pickup(other.gameObject);
        }
    }

    void Pickup(GameObject playerObj)
    {
        var pc = playerObj.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.AddGold(value);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("Gold: Player тぃ PlayerController礚猭 gold");
        }
    }
}

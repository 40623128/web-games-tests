using UnityEngine;
// using static UnityEditor.PlayerSettings;

public class Bullet : MonoBehaviour
{
    public float lifeTime = 3f;

    [Header("Borders")]
    public Transform bordersRoot;   // 指到場景的 Borders
    public float inset = 0.05f;     // 再往內縮一點，避免貼牆判斷抖動

    [Header("Piercing")]
    public int pierceCount = 0;          // ✅ 可穿透幾顆隕石（0=不穿透，1=可穿1顆...）

    [Header("VFX")]
    public GameObject vfxHitAsteroid;
    public float vfxHitScale = 1.0f;

    private int pierceLeft;
    private Bounds innerBounds;
    private bool hasBounds = false;

    private Collider2D myCol;
    private readonly System.Collections.Generic.HashSet<int> hitIds
        = new System.Collections.Generic.HashSet<int>();

    void Start()
    {
        myCol = GetComponent<Collider2D>();
        Destroy(gameObject, lifeTime);
        pierceLeft = Mathf.Max(0, pierceCount);

        // 自動找名為 Borders 的物件（你也可以在 Inspector 指定）
        if (bordersRoot == null)
        {
            var go = GameObject.Find("Borders");
            if (go != null) bordersRoot = go.transform;
        }

        if (bordersRoot != null)
        {
            innerBounds = CalculateInnerBoundsFromWalls(bordersRoot);
            hasBounds = innerBounds.size.x > 0f && innerBounds.size.y > 0f;
        }
        else
        {
            Debug.LogWarning("Bullet: 找不到 Borders，將只用 lifeTime 銷毀。");
        }
    }

    void Update()
    {
        if (!hasBounds) return;

        Vector3 p = transform.position;

        // 超出內側邊界就銷毀（防呆）
        if (p.x < innerBounds.min.x + inset || p.x > innerBounds.max.x - inset ||
            p.y < innerBounds.min.y + inset || p.y > innerBounds.max.y - inset)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // ✅ 撞到邊界就消失
        if (other.CompareTag("Border"))
        {
            Destroy(gameObject);
            return;
        }

        // ✅ 撞到隕石
        if (!other.CompareTag("Obstacle")) return;

        // ✅ 同一顆隕石只算一次（避免多 collider / 同幀多次觸發）
        int id = other.transform.root.gameObject.GetInstanceID();
        if (hitIds.Contains(id)) return;
        hitIds.Add(id);

        // ✅ 撞擊點（在 collider 表面最接近子彈的位置）
        Vector2 hitPoint = other.ClosestPoint(transform.position);

        // ✅ 在撞擊點生成特效
        if (vfxHitAsteroid != null)
        {
            var fx = Instantiate(vfxHitAsteroid, hitPoint, Quaternion.identity);
            fx.transform.localScale *= vfxHitScale;
        }

        // ✅ 讓隕石自己處理：爆炸 + 掉金 + 消失（把撞擊點傳進去）
        var obs = other.GetComponentInParent<Obstacle>();
        if (obs != null)
        {
            obs.HitByBullet(hitPoint);
        }

        // ✅ 穿透：還有次數就留下來
        if (pierceLeft > 0)
        {
            pierceLeft--;

            // 避免子彈卡在同一 collider 內一直觸發
            if (myCol != null)
                Physics2D.IgnoreCollision(myCol, other, true);

            return;
        }

        // ✅ 沒穿透次數了 -> 子彈消失
        Destroy(gameObject);
    }

    // 用四面牆 collider 的「內緣」算出真正的遊戲內部範圍
    static Bounds CalculateInnerBoundsFromWalls(Transform bordersRoot)
    {
        var cols = bordersRoot.GetComponentsInChildren<Collider2D>();
        if (cols == null || cols.Length < 4)
        {
            Debug.LogError("Borders 需要至少 4 個 Collider2D（上/下/左/右牆）");
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Collider2D top = cols[0], bottom = cols[0], left = cols[0], right = cols[0];

        foreach (var c in cols)
        {
            var p = c.bounds.center;
            if (p.y > top.bounds.center.y) top = c;
            if (p.y < bottom.bounds.center.y) bottom = c;
            if (p.x < left.bounds.center.x) left = c;
            if (p.x > right.bounds.center.x) right = c;
        }

        float innerMinX = left.bounds.max.x;     // 左牆內緣
        float innerMaxX = right.bounds.min.x;    // 右牆內緣
        float innerMinY = bottom.bounds.max.y;   // 下牆內緣
        float innerMaxY = top.bounds.min.y;      // 上牆內緣

        Vector3 center = new Vector3((innerMinX + innerMaxX) * 0.5f, (innerMinY + innerMaxY) * 0.5f, 0f);
        Vector3 size = new Vector3(Mathf.Abs(innerMaxX - innerMinX), Mathf.Abs(innerMaxY - innerMinY), 0f);

        return new Bounds(center, size);
    }
}

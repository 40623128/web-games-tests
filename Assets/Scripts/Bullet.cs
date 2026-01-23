using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifeTime = 3f;

    [Header("Borders")]
    public Transform bordersRoot;   // 指到場景的 Borders
    public float inset = 0.05f;     // 再往內縮一點，避免貼牆判斷抖動

    [Header("Hit Behavior")]
    public bool destroyObstacle = true; // 打到隕石是否要一起打掉

    private Bounds innerBounds;
    private bool hasBounds = false;

    void Start()
    {
        Destroy(gameObject, lifeTime);

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

        // ✅ 撞到隕石就消失（可選：順便打掉隕石）
        if (other.CompareTag("Obstacle"))
        {
            if (destroyObstacle) Destroy(other.gameObject);
            Destroy(gameObject);
            return;
        }
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

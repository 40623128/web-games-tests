using System.Collections;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Transform bordersRoot;      // Borders 物件
    public GameObject obstaclePrefab;
    public Transform player;           // 可選：避免生成在玩家附近

    [Header("Timing")]
    public float spawnInterval = 1.5f;

    [Header("Spawn Lane (inside)")]
    public float laneWidth = 1.0f;     // 內側巷道寬度
    public float inset = 0.05f;        // 再額外往內縮一點避免貼牆
    public float safeRadius = 1.5f;
    public int maxTry = 10;

    Bounds innerBounds;

    void Start()
    {
        innerBounds = CalculateInnerBoundsFromWalls(bordersRoot);
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (player != null)   // ✅ player 被 Destroy 後會變成 null
        {
            SpawnOne();
            yield return new WaitForSeconds(spawnInterval);
        }

        // 可選：確保停乾淨
        // Debug.Log("Player destroyed, stop spawning.");
    }

    void SpawnOne()
    {
        Vector3 pos = Vector3.zero;

        for (int i = 0; i < maxTry; i++)
        {
            pos = RandomPointInInnerLane(innerBounds, laneWidth, inset);

            if (player == null) break;
            if (Vector2.Distance(pos, player.position) >= safeRadius) break;
        }

        Instantiate(obstaclePrefab, pos, Quaternion.identity);
    }

    // 內側四周巷道：從 innerBounds 的四邊往內 laneWidth
    static Vector3 RandomPointInInnerLane(Bounds b, float laneWidth, float inset)
    {
        float minX = b.min.x + inset;
        float maxX = b.max.x - inset;
        float minY = b.min.y + inset;
        float maxY = b.max.y - inset;

        // laneWidth 不能比內部空間還大（保險）
        laneWidth = Mathf.Min(laneWidth, (maxX - minX) * 0.45f, (maxY - minY) * 0.45f);

        int edge = Random.Range(0, 4); // 0上1下2左3右
        switch (edge)
        {
            case 0: // 上邊內側
                return new Vector3(Random.Range(minX, maxX), Random.Range(maxY - laneWidth, maxY), 0f);
            case 1: // 下邊內側
                return new Vector3(Random.Range(minX, maxX), Random.Range(minY, minY + laneWidth), 0f);
            case 2: // 左邊內側
                return new Vector3(Random.Range(minX, minX + laneWidth), Random.Range(minY, maxY), 0f);
            default: // 右邊內側
                return new Vector3(Random.Range(maxX - laneWidth, maxX), Random.Range(minY, maxY), 0f);
        }
    }

    // 用四面牆 collider 的「內緣」算出真正的遊戲內部範圍
    static Bounds CalculateInnerBoundsFromWalls(Transform bordersRoot)
    {
        var cols = bordersRoot.GetComponentsInChildren<Collider2D>();
        if (cols == null || cols.Length < 4)
        {
            Debug.LogError("Borders 需要至少 4 個 Collider2D（上/下/左/右牆）");
            return new Bounds(Vector3.zero, Vector3.one * 10f);
        }

        // 用 collider 中心位置判斷哪個是 top/bottom/left/right
        Collider2D top = cols[0], bottom = cols[0], left = cols[0], right = cols[0];

        foreach (var c in cols)
        {
            var p = c.bounds.center;
            if (p.y > top.bounds.center.y) top = c;
            if (p.y < bottom.bounds.center.y) bottom = c;
            if (p.x < left.bounds.center.x) left = c;
            if (p.x > right.bounds.center.x) right = c;
        }

        // 內部邊界 = 牆的內緣
        float innerMinX = left.bounds.max.x;     // 左牆的右邊緣
        float innerMaxX = right.bounds.min.x;    // 右牆的左邊緣
        float innerMinY = bottom.bounds.max.y;   // 下牆的上邊緣
        float innerMaxY = top.bounds.min.y;      // 上牆的下邊緣

        Vector3 center = new Vector3((innerMinX + innerMaxX) * 0.5f, (innerMinY + innerMaxY) * 0.5f, 0f);
        Vector3 size = new Vector3(Mathf.Abs(innerMaxX - innerMinX), Mathf.Abs(innerMaxY - innerMinY), 0f);

        if (size.x <= 0 || size.y <= 0)
        {
            Debug.LogError("計算 innerBounds 失敗：可能 Borders 牆的排列或方向不對");
            return new Bounds(Vector3.zero, Vector3.one * 10f);
        }

        return new Bounds(center, size);
    }
}

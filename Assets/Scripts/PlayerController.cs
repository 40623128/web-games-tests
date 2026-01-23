using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{

    [Header("Collect")]
    public int gold = 0;
    private Label goldText;


    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 12f;
    public float shotInterval = 0.12f;
    public int shotsBeforeCooldown = 10;
    public float cooldownTime = 1.5f;
    public bool holdToFire = true;


    private float shotTimer = 0f;
    private float cooldownTimer = 0f;
    private int shotsLeft = 0;

    [Header("Score")]
    private float elapsedTime = 0f;
    private float score = 0f;
    public float scoreMultiplier = 10f;

    [Header("Movement")]
    public float thrustForce = 1f;
    public float maxSpeed = 5f;
    public GameObject boosterFlame;

    [Header("UI")]
    public UIDocument uiDocument;
    private Label scoreText;
    private Button restartButton;

    [Header("VFX")]
    public GameObject explosionEffect;

    private Rigidbody2D rb;
    private Collider2D playerCol;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCol = GetComponent<Collider2D>();

        shotsLeft = Mathf.Max(1, shotsBeforeCooldown);

        if (uiDocument != null)
        {
            scoreText = uiDocument.rootVisualElement.Q<Label>("ScoreLabel");
            restartButton = uiDocument.rootVisualElement.Q<Button>("RestartButton");
            if (restartButton != null)
            {
                restartButton.style.display = DisplayStyle.None;
                restartButton.clicked += ReloadScene;
            }
        }
        goldText = uiDocument.rootVisualElement.Q<Label>("GoldCount");
        UpdateGoldUI();

        if (firePoint == null)
        {
            Transform fp = transform.Find("FirePoint");
            if (fp != null) firePoint = fp;
        }
    }

    void Update()
    {
        UpdateScore();
        MovePlayer();
        HandleShooting();
    }

    void UpdateScore()
    {
        elapsedTime += Time.deltaTime;
        score = Mathf.FloorToInt(elapsedTime * scoreMultiplier);
        if (scoreText != null) scoreText.text = "Score: " + score;
    }

    void MovePlayer()
    {
        if (Mouse.current == null || Camera.main == null) return;

        // 1) 永遠讓飛船頭朝向滑鼠
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.value);
        mousePos.z = 0f;
        Vector2 direction = ((Vector2)(mousePos - transform.position)).normalized;

        if (direction.sqrMagnitude > 0.0001f)
            transform.up = direction;

        // 2) 按住左鍵時持續推進
        if (Mouse.current.leftButton.isPressed)
        {
            // 持續施力（每幀推）
            rb.AddForce(direction * thrustForce, ForceMode2D.Force);

            // 限速
            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

            boosterFlame.SetActive(true);
        }
        else
        {
            boosterFlame.SetActive(false);
        }
    }



    void HandleShooting()
    {
        if (Mouse.current == null) return;

        shotTimer = Mathf.Max(0f, shotTimer - Time.deltaTime);

        if (cooldownTimer > 0f)
        {
            cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
            if (cooldownTimer <= 0f)
            {
                shotsLeft = Mathf.Max(1, shotsBeforeCooldown);
                shotTimer = 0f;
            }
            return;
        }

        if (shotsLeft <= 0)
        {
            cooldownTimer = cooldownTime;
            return;
        }

        bool wantFire = holdToFire
            ? Mouse.current.rightButton.isPressed
            : Mouse.current.rightButton.wasPressedThisFrame;

        if (!wantFire) return;
        if (shotTimer > 0f) return;

        FireOne();
        shotTimer = shotInterval;
        shotsLeft--;

        if (shotsLeft <= 0)
        {
            cooldownTimer = cooldownTime;
            shotTimer = 0f;
        }
    }

    void FireOne()
    {
        if (bulletPrefab == null) return;

        Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
        GameObject b = Instantiate(bulletPrefab, spawnPos, transform.rotation);

        Rigidbody2D brb = b.GetComponent<Rigidbody2D>();
        if (brb != null)
            brb.linearVelocity = (Vector2)transform.up * bulletSpeed;

        // ✅ 避免子彈誤殺自己（即使你之後改規則也安全）
        if (playerCol != null)
        {
            Collider2D bulletCol = b.GetComponent<Collider2D>();
            if (bulletCol != null)
                Physics2D.IgnoreCollision(bulletCol, playerCol, true);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // ✅ 撞到隕石或邊界就死
        if (collision.collider.CompareTag("Obstacle") || collision.collider.CompareTag("Border"))
        {
            Die();
        }
    }

    // 如果你邊界用 Trigger，這個也一起支援
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle") || other.CompareTag("Border"))
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, transform.rotation);

        if (restartButton != null)
            restartButton.style.display = DisplayStyle.Flex;
    }

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void AddGold(int amount)
    {
        gold += amount;
        UpdateGoldUI();
    }

    void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = "Gold: " + gold;
    }
}

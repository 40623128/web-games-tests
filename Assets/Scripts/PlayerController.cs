using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource thrustSource;   // 用來播噴射(Loop)
    public AudioClip thrustClip;
    [Range(0f, 1f)] public float thrustVolume = 0.6f;

    private bool wasThrusting = false;


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

    [Header("Bullet Upgrades")]
    public int bulletPierce = 0; // 0=不穿透，1=可穿1顆...

    [Header("UI")]
    public UIDocument uiDocument;
    private Label scoreText;
    private Button restartButton;

    [Header("VFX")]
    public GameObject explosionEffect;

    private Rigidbody2D rb;
    private Collider2D playerCol;

    public bool IsAlive { get; private set; } = true;
    public int CurrentScore => Mathf.FloorToInt(elapsedTime * scoreMultiplier);


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

        // ===== Thrust audio init =====
        if (thrustSource == null)
        {
            // 你可以直接用玩家身上的 AudioSource
            thrustSource = GetComponent<AudioSource>();
            if (thrustSource == null) thrustSource = gameObject.AddComponent<AudioSource>();
        }

        thrustSource.playOnAwake = false;
        thrustSource.loop = true;
        thrustSource.clip = thrustClip;
        thrustSource.volume = thrustVolume;

    }

    void Update()
    {
        if (!IsAlive) return;

        // ✅ 升級選單暫停中：關掉噴射火焰 + 不處理移動/射擊
        if (Time.timeScale == 0f)
        {
            if (boosterFlame != null) boosterFlame.SetActive(false);

            if (thrustSource != null && thrustSource.isPlaying)
                thrustSource.Stop();

            wasThrusting = false;
            return;
        }

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
        bool thrusting = Mouse.current.leftButton.isPressed;

        if (thrusting)
        {
            rb.AddForce(direction * thrustForce, ForceMode2D.Force);

            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

            if (boosterFlame != null) boosterFlame.SetActive(true);

            // ===== play thrust sound (loop) =====
            if (!wasThrusting)
            {
                if (thrustSource != null && thrustClip != null)
                {
                    thrustSource.volume = thrustVolume;
                    if (thrustSource.clip != thrustClip) thrustSource.clip = thrustClip;
                    thrustSource.Play();
                }
                wasThrusting = true;
            }
        }
        else
        {
            if (boosterFlame != null) boosterFlame.SetActive(false);

            // ===== stop thrust sound =====
            if (wasThrusting)
            {
                if (thrustSource != null && thrustSource.isPlaying)
                    thrustSource.Stop();
                wasThrusting = false;
            }
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

        var bullet = b.GetComponent<Bullet>();
        if (bullet != null)
            bullet.pierceCount = bulletPierce;

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
        IsAlive = false;
        Destroy(gameObject);
        if (thrustSource != null && thrustSource.isPlaying)
            thrustSource.Stop();

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
    public void ApplyUpgrade(UpgradeType type, float value)
    {
        switch (type)
        {
            case UpgradeType.ThrustUp:
                thrustForce *= (1f + value); // value=0.25 => +25%
                break;

            case UpgradeType.MaxSpeedUp:
                maxSpeed *= (1f + value);
                break;

            case UpgradeType.BulletSpeedUp:
                bulletSpeed *= (1f + value);
                break;

            case UpgradeType.FireRateUp:
                // value 是負數：shotInterval 變小 => 更快
                shotInterval = Mathf.Max(0.03f, shotInterval * value);
                break;

            case UpgradeType.MagSizeUp:
                shotsBeforeCooldown += Mathf.RoundToInt(value);
                shotsBeforeCooldown = Mathf.Max(1, shotsBeforeCooldown);
                // 你也可以選擇立刻補滿：
                // shotsLeft = shotsBeforeCooldown;
                break;

            case UpgradeType.CooldownDown:
                cooldownTime = Mathf.Max(0.1f, cooldownTime * value); // value=-0.2 => 冷卻變短
                break;

            case UpgradeType.PierceUp:
                bulletPierce += Mathf.RoundToInt(value);   // value=1 => 穿透+1
                bulletPierce = Mathf.Clamp(bulletPierce, 0, 50); // 防呆上限
                break;
        }

        Debug.Log($"Upgrade applied: {type} ({value})");
    }

    public void RefreshGoldUI()
    {
        goldText.text = "Gold:" + gold;
    }

}

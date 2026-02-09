using System.Collections;
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

    [Header("Multi Shot")]
    public int bulletsPerShot = 1;       // 1=單發, 2=雙發(之後也可做3發)
    public float multiShotSpacing = 0.18f; // 兩顆子彈左右間距（世界座標）

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

    [Header("Health")]
    public int maxLives = 3;
    public int lives = 3;
    public float invincibleTime = 1.0f;     // 受傷後無敵秒數
    public float blinkInterval = 0.1f;      // 閃爍間隔
    private bool invincible = false;
    private Coroutine invincibleCo;

    [Header("UI")]
    public UIDocument uiDocument;
    private Label scoreText;
    private Button restartButton;
    private Label livesText;

    [Header("VFX")]
    public GameObject vfxPlayerHurt;   // ✅ 飛船受傷特效
    public GameObject vfxPlayerDeath;  // ✅ 飛船死亡特效
    public GameObject explosionEffect; // (可選) 你原本的爆炸

    public float vfxHurtScale = 1.0f;
    public float vfxDeathScale = 1.6f;



    private Rigidbody2D rb;
    private Collider2D playerCol;
    private SpriteRenderer sr;

    public bool IsAlive { get; private set; } = true;
    public int CurrentScore => Mathf.FloorToInt(elapsedTime * scoreMultiplier);

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCol = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        shotsLeft = Mathf.Max(1, shotsBeforeCooldown);

        if (uiDocument != null)
        {
            scoreText = uiDocument.rootVisualElement.Q<Label>("ScoreLabel");
            restartButton = uiDocument.rootVisualElement.Q<Button>("RestartButton");
            goldText = uiDocument.rootVisualElement.Q<Label>("GoldCount");
            livesText = uiDocument.rootVisualElement.Q<Label>("LivesLabel"); // ✅ 新增這個 Label

            if (restartButton != null)
            {
                restartButton.style.display = DisplayStyle.None;
                restartButton.clicked += ReloadScene;
            }
        }

        UpdateGoldUI();

        lives = Mathf.Clamp(lives, 1, maxLives);
        UpdateLivesUI();

        if (firePoint == null)
        {
            Transform fp = transform.Find("FirePoint");
            if (fp != null) firePoint = fp;
        }

        // ===== Thrust audio init =====
        if (thrustSource == null)
        {
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

        Vector3 basePos = (firePoint != null) ? firePoint.position : transform.position;
        Vector2 forward = transform.up;     // 子彈前進方向
        Vector2 right = transform.right;  // 用來左右偏移

        int n = Mathf.Clamp(bulletsPerShot, 1, 10);

        // n=1 => offset=0
        // n=2 => offset=-0.5, +0.5
        // n=3 => offset=-1, 0, +1 ...
        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0f : (i - (n - 1) * 0.5f);  // 置中分布
            Vector3 spawnPos = basePos + (Vector3)(right * (t * multiShotSpacing));

            GameObject b = Instantiate(bulletPrefab, spawnPos, transform.rotation);
            BGMManager.Instance?.PlayShoot();

            Rigidbody2D brb = b.GetComponent<Rigidbody2D>();
            if (brb != null)
                brb.linearVelocity = forward * bulletSpeed;

            var bullet = b.GetComponent<Bullet>();
            if (bullet != null)
                bullet.pierceCount = bulletPierce;

            // ✅ 避免子彈誤殺自己
            if (playerCol != null)
            {
                Collider2D bulletCol = b.GetComponent<Collider2D>();
                if (bulletCol != null)
                    Physics2D.IgnoreCollision(bulletCol, playerCol, true);
            }
        }
    }


    // =========================
    // Damage / Lives
    // =========================
    public void TakeDamage(int dmg = 1)
    {
        if (!IsAlive) return;
        if (invincible) return;

        lives -= dmg;
        if (lives < 0) lives = 0;
        UpdateLivesUI();

        // 受傷貓掌（小
        if (lives > 0 && vfxPlayerHurt != null)
        {
            var go = Instantiate(vfxPlayerHurt, transform.position, Quaternion.identity);
            go.transform.localScale *= vfxHurtScale;
        }
        if (lives <= 0)
        {
            Die();
            return;
        }

        // 進入無敵 + 閃爍
        if (invincibleCo != null) StopCoroutine(invincibleCo);
        invincibleCo = StartCoroutine(InvincibleCoroutine(invincibleTime));
    }

    private IEnumerator InvincibleCoroutine(float t)
    {
        invincible = true;

        float elapsed = 0f;
        while (elapsed < t)
        {
            elapsed += blinkInterval;

            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }

        if (sr != null) sr.enabled = true;
        invincible = false;
        invincibleCo = null;
    }

    void UpdateLivesUI()
    {
        if (livesText != null)
            livesText.text = ": " + lives;
    }

    // =========================
    // Collision => TakeDamage
    // =========================
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Obstacle") || collision.collider.CompareTag("Border"))
        {
            TakeDamage(1);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Obstacle") || other.CompareTag("Border"))
        {
            TakeDamage(1);
        }
    }

    void Die()
    {
        IsAlive = false;
        BGMManager.Instance?.PlayPlayerDeath();
        if (thrustSource != null && thrustSource.isPlaying)
            thrustSource.Stop();

        if (boosterFlame != null) boosterFlame.SetActive(false);

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, transform.rotation);
        
        // ✅ 死亡特效（優先用 vfxPlayerDeath，沒有就用原本 explosionEffect）
        GameObject deathFx = vfxPlayerDeath != null ? vfxPlayerDeath : explosionEffect;
        if (deathFx != null)
        {
            var go = Instantiate(deathFx, transform.position, Quaternion.identity);
            go.transform.localScale *= vfxDeathScale;
        }

        if (restartButton != null)
            restartButton.style.display = DisplayStyle.Flex;

        // ✅ 想保留屍體/避免再撞：關掉碰撞與顯示即可（比 Destroy 更穩）
        //if (playerCol != null) playerCol.enabled = false;
        //if (sr != null) sr.enabled = false;

        // 如果你真的想直接刪掉玩家，改成 Destroy(gameObject);
        Destroy(gameObject);
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
            goldText.text = ": " + gold;
    }

    public void ApplyUpgrade(UpgradeType type, float value)
    {
        switch (type)
        {
            case UpgradeType.ThrustUp:
                thrustForce *= (1f + value);
                break;

            case UpgradeType.MaxSpeedUp:
                maxSpeed *= (1f + value);
                break;

            case UpgradeType.BulletSpeedUp:
                bulletSpeed *= (1f + value);
                break;

            case UpgradeType.FireRateUp:
                shotInterval = Mathf.Max(0.03f, shotInterval * value);
                break;

            case UpgradeType.MagSizeUp:
                shotsBeforeCooldown += Mathf.RoundToInt(value);
                shotsBeforeCooldown = Mathf.Max(1, shotsBeforeCooldown);
                break;

            case UpgradeType.CooldownDown:
                cooldownTime = Mathf.Max(0.1f, cooldownTime * value);
                break;

            case UpgradeType.PierceUp:
                bulletPierce += Mathf.RoundToInt(value);
                bulletPierce = Mathf.Clamp(bulletPierce, 0, 50);
                break;
            case UpgradeType.LifeUp:
                AddLifeAndMax(Mathf.RoundToInt(value));   // value=1 => +1 命
                break;
            case UpgradeType.MultiShot:
                bulletsPerShot = Mathf.Clamp(bulletsPerShot + Mathf.RoundToInt(value), 1, 5);
                break;
        }

        Debug.Log($"Upgrade applied: {type} ({value})");
    }

    public void RefreshGoldUI()
    {
        if (goldText != null)
            goldText.text = ":" + gold;
    }


    public void AddLifeAndMax(int amount = 1)
    {
        maxLives += amount;
        lives = Mathf.Clamp(lives + amount, 0, maxLives);
        UpdateLivesUI();
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UpgradeByGold : MonoBehaviour
{
    [Header("Refs")]
    public UIDocument uiDocument;
    public PlayerController player;

    [Header("Gold Trigger")]
    public int firstUpgradeGold = 5;           // 第一次升級需要多少 gold
    public bool spendGoldWhenUpgrade = true;   // 觸發升級時是否扣金幣（建議 true）

    [Header("Gold Cost Curve")]
    public int incrementStart = 10;            // 5->15 的增量 = 10
    public int incrementStep = 5;              // 增量每次再 +5（10,15,20...）

    private int nextUpgradeGold;
    private int nextIncrement;

    private bool choosing = false;

    // UI
    private VisualElement group;
    private Button b1, b2, b3;

    // 升級池
    private List<Option> options = new();

    void Start()
    {
        if (uiDocument == null) uiDocument = FindFirstObjectByType<UIDocument>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (uiDocument == null)
        {
            Debug.LogError("[UpgradeByGold] UIDocument not found in scene.");
            enabled = false;
            return;
        }
        if (player == null)
        {
            Debug.LogError("[UpgradeByGold] PlayerController not found in scene.");
            enabled = false;
            return;
        }

        var root = uiDocument.rootVisualElement;

        group = root.Q<VisualElement>("UpgradeButtonGroup");
        b1 = root.Q<Button>("UpgradeButton1");
        b2 = root.Q<Button>("UpgradeButton2");
        b3 = root.Q<Button>("UpgradeButton3");

        if (group == null || b1 == null || b2 == null || b3 == null)
        {
            Debug.LogError("[UpgradeByGold] Upgrade UI not found. Check UXML names: UpgradeButtonGroup / UpgradeButton1 / 2 / 3");
            enabled = false;
            return;
        }

        group.style.display = DisplayStyle.None;

        nextUpgradeGold = Mathf.Max(1, firstUpgradeGold);
        nextIncrement = Mathf.Max(1, incrementStart);

        BuildOptions();
    }

    void Update()
    {
        if (player == null || !player.IsAlive) return;
        if (choosing) return;

        int gold = player.gold;

        if (gold >= nextUpgradeGold)
        {
            // 觸發升級就先扣款（避免連續跳窗）
            if (spendGoldWhenUpgrade)
            {
                player.gold -= nextUpgradeGold;
                if (player.gold < 0) player.gold = 0;

                // 你有 UI 更新函式就呼叫；沒有就刪掉這行
                player.RefreshGoldUI();
            }

            ShowMenu();

            // ✅ 門檻序列：5, 15, 30, 50, 75...
            nextUpgradeGold += nextIncrement;         // +10, +15, +20...
            nextIncrement += Mathf.Max(1, incrementStep);
        }
    }

    void BuildOptions()
    {
        options = new List<Option>
        {
            new Option(UpgradeType.ThrustUp,        0.20f, "Thrust +20%",        1.0f),
            new Option(UpgradeType.MaxSpeedUp,      0.20f, "Max Speed +20%",     1.0f),

            new Option(UpgradeType.BulletSpeedUp,   0.20f, "Bullet Speed +20%",  1.0f),
            new Option(UpgradeType.FireRateUp,      0.95f, "Firing Rate +5%",    1.0f),
            new Option(UpgradeType.CooldownDown,    0.90f, "Reload speed +10%",  1.0f),

            new Option(UpgradeType.LifeUp,          1.00f, "Life +1",            0.1f),
            new Option(UpgradeType.PierceUp,        1.00f, "Pierce +1",          0.1f),
            new Option(UpgradeType.MagSizeUp,       1.00f, "Ammo Capacity +1",   0.1f),
            new Option(UpgradeType.MultiShot,       1.00f, "MultiShot +1",       0.1f),
        };
    }

    void ShowMenu()
    {
        choosing = true;

        // 暫停遊戲（UI Toolkit 還是能點）
        Time.timeScale = 0f;

        group.style.display = DisplayStyle.Flex;

        // ✅ 升級選單跳出音效
        BGMManager.Instance?.PlayUpgradeOpen();

        var picks = Pick3Weighted(options);

        // 保險：不足 3 個時做補齊
        while (picks.Count < 3 && options.Count > 0)
            picks.Add(options[UnityEngine.Random.Range(0, options.Count)]);

        SetupButton(b1, picks[0]);
        SetupButton(b2, picks[1]);
        SetupButton(b3, picks[2]);
    }

    void SetupButton(Button btn, Option opt)
    {
        btn.text = opt.text;

        // 清掉舊事件（避免疊加）
        if (btn.userData is Action oldAct) btn.clicked -= oldAct;

        Action act = () =>
        {
            if (player != null && player.IsAlive)
                player.ApplyUpgrade(opt.type, opt.value);

            // ✅ 選到升級音效
            BGMManager.Instance?.PlayUpgradeChoose();

            CloseMenu();
        };

        btn.userData = act;
        btn.clicked += act;
    }

    void CloseMenu()
    {
        group.style.display = DisplayStyle.None;

        choosing = false;
        Time.timeScale = 1f;
    }

    static List<Option> Pick3Weighted(List<Option> src)
    {
        // 複製一份候選（不放回抽樣用）
        var pool = new List<Option>(src);
        var res = new List<Option>(3);

        for (int pick = 0; pick < 3 && pool.Count > 0; pick++)
        {
            float total = 0f;
            for (int i = 0; i < pool.Count; i++)
                total += Mathf.Max(0f, pool[i].weight);

            // 全部權重為 0 -> 退回等機率抽
            if (total <= 0f)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                res.Add(pool[idx]);
                pool.RemoveAt(idx);
                continue;
            }

            float r = UnityEngine.Random.value * total;
            float acc = 0f;
            int chosen = pool.Count - 1;

            for (int i = 0; i < pool.Count; i++)
            {
                acc += Mathf.Max(0f, pool[i].weight);
                if (r <= acc)
                {
                    chosen = i;
                    break;
                }
            }

            res.Add(pool[chosen]);
            pool.RemoveAt(chosen); // ✅ 不放回：移除已選
        }

        return res;
    }

    struct Option
    {
        public UpgradeType type;
        public float value;
        public string text;
        public float weight; // 權重（機率用）

        public Option(UpgradeType t, float v, string s, float w)
        {
            type = t;
            value = v;
            text = s;
            weight = Mathf.Max(0f, w);
        }
    }
}

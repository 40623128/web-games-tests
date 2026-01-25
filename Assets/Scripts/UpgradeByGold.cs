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
    public int firstUpgradeGold = 5;     // 第一次升級需要多少 gold
    public int upgradeEveryGold = 5;     // 之後每多多少 gold 再升級一次
    public bool spendGoldWhenUpgrade = true; // 觸發升級時是否扣金幣（建議 true）

    [Header("Gold Cost Curve")]
    public int incrementStart = 10;      // 5->15 的增量 = 10
    public int incrementStep = 5;        // 增量每次再 +5（10,15,20...）

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
        if (uiDocument == null) uiDocument = FindObjectOfType<UIDocument>();
        if (player == null) player = FindObjectOfType<PlayerController>();

        var root = uiDocument.rootVisualElement;

        group = root.Q<VisualElement>("UpgradeButtonGroup");
        b1 = root.Q<Button>("UpgradeButton1");
        b2 = root.Q<Button>("UpgradeButton2");
        b3 = root.Q<Button>("UpgradeButton3");

        if (group != null) group.style.display = DisplayStyle.None;

        nextUpgradeGold = firstUpgradeGold; // 5
        nextIncrement = incrementStart;   // 10


        nextUpgradeGold = Mathf.Max(1, firstUpgradeGold);

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
            nextUpgradeGold += nextIncrement;   // +10, +15, +20...
            nextIncrement += incrementStep;   // 每次增量再 +5
        }
    }



    void BuildOptions()
    {
        options = new List<Option>
        {
            new Option(UpgradeType.ThrustUp,      0.20f, "Thrust +20%"),
            new Option(UpgradeType.MaxSpeedUp,    0.20f, "Max Speed +20%"),
            new Option(UpgradeType.BulletSpeedUp, 0.20f, "Bullet Speed +20%"),
            new Option(UpgradeType.FireRateUp,   0.95f, "Firing Rate +5%"),
            new Option(UpgradeType.MagSizeUp,      1f,   "Capacity +1"),
            new Option(UpgradeType.CooldownDown, 0.9f,  "Reload speed +10%"),
            new Option(UpgradeType.PierceUp, 1f, "Pierce +1"),
        };
    }

    void ShowMenu()
    {
        if (group == null || b1 == null || b2 == null || b3 == null)
        {
            Debug.LogError("Upgrade UI not found. Check UXML names: UpgradeButtonGroup/1/2/3");
            return;
        }

        choosing = true;
        Time.timeScale = 0f;

        group.style.display = DisplayStyle.Flex;

        var picks = Pick3(options);

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

            CloseMenu();
        };

        btn.userData = act;
        btn.clicked += act;
    }

    void CloseMenu()
    {
        if (group != null) group.style.display = DisplayStyle.None;

        choosing = false;
        Time.timeScale = 1f;
    }

    static List<Option> Pick3(List<Option> src)
    {
        var chosen = new HashSet<int>();
        while (chosen.Count < 3) chosen.Add(UnityEngine.Random.Range(0, src.Count));

        var res = new List<Option>(3);
        foreach (var i in chosen) res.Add(src[i]);
        return res;
    }

    struct Option
    {
        public UpgradeType type;
        public float value;
        public string text;

        public Option(UpgradeType t, float v, string s)
        {
            type = t; value = v; text = s;
        }
    }
}

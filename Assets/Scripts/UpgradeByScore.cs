using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UpgradeByScore : MonoBehaviour
{
    [Header("Refs")]
    public UIDocument uiDocument;
    public PlayerController player;

    [Header("Score Trigger")]
    public int firstUpgradeScore = 100;   // 第一次升級分數
    public int upgradeEveryScore = 100;   // 之後每 +N 分升級一次

    private int nextUpgradeScore;
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

        var root = uiDocument.rootVisualElement;

        group = root.Q<VisualElement>("UpgradeButtonGroup");
        b1 = root.Q<Button>("UpgradeButton1");
        b2 = root.Q<Button>("UpgradeButton2");
        b3 = root.Q<Button>("UpgradeButton3");

        // 先隱藏
        if (group != null) group.style.display = DisplayStyle.None;

        nextUpgradeScore = firstUpgradeScore;

        BuildOptions();
    }

    void Update()
    {
        if (player == null || !player.IsAlive) return;
        if (choosing) return;

        // 這裡直接從 PlayerController 讀分數
        int score = player.CurrentScore;

        if (score >= nextUpgradeScore)
        {
            ShowMenu();
            nextUpgradeScore += upgradeEveryScore;
        }
    }

    void BuildOptions()
    {
        // 你可以自己調整內容與數值
        options = new List<Option>
        {
            new Option(UpgradeType.ThrustUp,     0.25f, "推進力 +25%"),
            new Option(UpgradeType.MaxSpeedUp,   0.20f, "最高速度 +20%"),
            new Option(UpgradeType.BulletSpeedUp,0.25f, "子彈速度 +25%"),
            new Option(UpgradeType.FireRateUp,  -0.02f, "射速更快 (間隔 -0.02s)"),
            new Option(UpgradeType.MagSizeUp,     2f,   "彈匣容量 +2"),
            new Option(UpgradeType.CooldownDown,-0.2f,  "冷卻時間 -0.2s"),
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
        Time.timeScale = 0f; // 暫停遊戲

        group.style.display = DisplayStyle.Flex;

        // 隨機抽三個不重複
        var picks = Pick3(options);

        SetupButton(b1, picks[0]);
        SetupButton(b2, picks[1]);
        SetupButton(b3, picks[2]);
    }

    void SetupButton(Button btn, Option opt)
    {
        btn.text = opt.text;

        // 清掉舊事件（避免疊加）
        btn.clicked -= btn.userData as Action;

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

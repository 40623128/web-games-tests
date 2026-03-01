using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names")]
    public string gameSceneName = "Game";

    [Header("Optional")]
    public string versionText = "v0.6";

    private UIDocument doc;
    private VisualElement root;

    void Awake()
    {
        doc = GetComponent<UIDocument>();
        root = doc.rootVisualElement;

        var btnStart = root.Q<Button>("BtnStart");
        var btnHow = root.Q<Button>("BtnHow");
        var btnQuit = root.Q<Button>("BtnQuit");
        var version = root.Q<Label>("Version");

        if (version != null) version.text = versionText;

        if (btnStart != null) btnStart.clicked += StartGame;
        if (btnHow != null) btnHow.clicked += ShowHowToPlay;
        if (btnQuit != null) btnQuit.clicked += QuitGame;
    }

    void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    void ShowHowToPlay()
    {
        if (root == null) return;

        // 再按一次就關閉
        var existing = root.Q<VisualElement>("HowOverlay");
        if (existing != null)
        {
            existing.RemoveFromHierarchy();
            return;
        }

        var overlay = new VisualElement
        {
            name = "HowOverlay"
        };
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.top = 0;
        overlay.style.right = 0;
        overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;

        var box = new VisualElement();
        box.style.width = 520;
        box.style.paddingLeft = 24;
        box.style.paddingRight = 24;
        box.style.paddingTop = 20;
        box.style.paddingBottom = 20;
        box.style.backgroundColor = new Color(0, 0, 0, 0.9f);
        box.style.borderTopLeftRadius = 16;
        box.style.borderTopRightRadius = 16;
        box.style.borderBottomLeftRadius = 16;
        box.style.borderBottomRightRadius = 16;

        var txt = new Label(
            "遊戲說明\n" +
            "- 滑鼠右鍵：火箭加速\n" +
            "- 滑鼠左鍵：射擊\n" +
            "- 收集貓糧：升級\n\n" +
            "點任意處關閉"
        );
        txt.style.whiteSpace = WhiteSpace.Normal;
        txt.style.unityTextAlign = TextAnchor.MiddleCenter;
        txt.style.fontSize = 18;

        // ✅ 文字顏色與描邊（UI Toolkit 要 0~1）
        txt.style.color = Color.white;
        txt.style.unityTextOutlineColor = Color.black;       // 或 new Color(0,0,0,1)
        txt.style.unityTextOutlineWidth = 1;                 // 沒有這個就看不到描邊

        // ✅ 行距、邊距（可選）
        txt.style.marginTop = 4;
        txt.style.marginBottom = 4;

        box.Add(txt);
        overlay.Add(box);

        // 點任意處關閉（包含 box）
        overlay.RegisterCallback<ClickEvent>(_ => overlay.RemoveFromHierarchy());

        root.Add(overlay);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
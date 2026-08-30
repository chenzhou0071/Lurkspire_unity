using UnityEngine;
using UnityEngine.UI;

// HUD — 状态显示（运行时自动生成，免搭建）
// 左下：HP 条+数字 / LOCK 充能条 / DASH 冷却条
// 右下：AMMO 弹药 / BLOCK 格挡条
public class HUD : MonoBehaviour
{
    private const float BarWidth = 220f;
    private const float BarHeight = 14f;
    private const float LockSegWidth = 70f;   // 锁头单格宽度
    private const float LockSegGap = 4f;
    private Image _hpBar, _dashBar, _blockBar;
    private Image[] _lockSegments; // 锁头 3 格（当前格显示连续充能进度）
    private Text _hpText, _ammoText, _lockText;
    private GunView _gunView;
    private SwordView _swordView;
    private HealthComponent _playerHealth;
    private float _lockFlashText;

    private void Awake()
    {
        var canvasGO = new GameObject("HUDCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();

        // ---- 左下：HP / LOCK（3 格）/ DASH ----
        _hpBar = CreateBar(canvasGO.transform, "HPBar", new Color(0.85f, 0.2f, 0.2f, 0.55f), new Vector2(0, 0), new Vector2(16, 16));
        _hpText = CreateText(canvasGO.transform, "HPText", new Vector2(0, 0), new Vector2(250, 40), new Vector2(26, 10), TextAnchor.LowerLeft, 26);
        _lockSegments = new Image[GameConfig.ChargeMax];
        for (int i = 0; i < GameConfig.ChargeMax; i++)
        {
            _lockSegments[i] = CreateBar(canvasGO.transform, $"LockSeg{i}",
                new Color(0.95f, 0.8f, 0.2f, 0.55f), new Vector2(0, 0),
                new Vector2(16 + i * (LockSegWidth + LockSegGap), 42));
        }
        _dashBar = CreateBar(canvasGO.transform, "DashBar", new Color(0.3f, 0.9f, 0.4f, 0.55f), new Vector2(0, 0), new Vector2(16, 68));

        // ---- 右下：AMMO / BLOCK ----
        _ammoText = CreateText(canvasGO.transform, "AmmoText", new Vector2(1, 0), new Vector2(-16, 16), new Vector2(200, 40), TextAnchor.LowerRight, 30);
        _blockBar = CreateBar(canvasGO.transform, "BlockBar", new Color(0.3f, 0.6f, 0.95f, 0.55f), new Vector2(1, 0), new Vector2(-16, 60));

        // ---- 锁头提示（屏幕中心上方） ----
        _lockText = CreateText(canvasGO.transform, "LockText", new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(300, 50), TextAnchor.MiddleCenter, 26);
        _lockText.text = "<< LOCK! >>";
        _lockText.color = Color.yellow;
        _lockText.gameObject.SetActive(false);

        _gunView = FindFirstObjectByType<GunView>();
        _swordView = FindFirstObjectByType<SwordView>();
        var player = FindFirstObjectByType<PlayerInput>();
        if (player != null) _playerHealth = player.GetComponent<HealthComponent>();
    }

    private void Update()
    {
        _lockFlashText -= Time.deltaTime;
        // 血量（条 + 数字）
        float hp = _playerHealth != null ? _playerHealth.Logic.HP : 0f;
        SetBar(_hpBar, hp / GameConfig.MaxHealth);
        _hpText.text = $"{(int)hp} ({(int)(hp)}/{GameConfig.MaxHealth})";
        // 锁头充能：3 格分开，当前格显示连续进度（慢慢积累）
        int charge = _gunView != null ? _gunView.Charge : 0;
        float progress = _gunView != null ? _gunView.ChargeProgress : 0f;
        for (int i = 0; i < _lockSegments.Length; i++)
        {
            float fill;
            if (i < charge) fill = 1f;                          // 已充好的格：满
            else if (i == charge && charge < GameConfig.ChargeMax) fill = progress; // 当前格：连续进度
            else fill = 0f;                                     // 未到的格：空
            _lockSegments[i].rectTransform.sizeDelta =
                new Vector2(LockSegWidth * Mathf.Clamp01(fill), BarHeight);
        }
        // 冲刺冷却（READY 满条；冷却中按剩余比例）
        float dashFill = 1f;
        if (_swordView != null && _swordView.DashCooldownRemain > 0f)
            dashFill = 1f - _swordView.DashCooldownRemain / GameConfig.DashAttackCooldown;
        SetBar(_dashBar, dashFill);
        // 弹药
        _ammoText.text = _gunView != null ? $"{_gunView.Ammo} / {GameConfig.MagazineSize}" : "--";
        // 格挡条
        SetBar(_blockBar, _swordView != null ? _swordView.Block / GameConfig.BlockMax : 0f);
        // 锁头提示
        if (_lockFlashText > 0f)
        {
            _lockFlashText -= Time.deltaTime;
            _lockText.gameObject.SetActive(true);
        }
        else
        {
            _lockText.gameObject.SetActive(false);
        }
    }

    // 锁头提示（GunView 发射锁头时调用）
    public void ShowLockFlash() => _lockFlashText = 0.8f;

    // 条：宽度按比例（fill 0~1），锚点侧固定（左条左伸/右条右伸）
    private void SetBar(Image bar, float fill)
    {
        if (bar == null) return;
        var rt = bar.rectTransform;
        rt.sizeDelta = new Vector2(BarWidth * Mathf.Clamp01(fill), BarHeight);
    }

    private static Image CreateBar(Transform parent, string name, Color color,
        Vector2 anchor, Vector2 offset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(BarWidth, BarHeight);
        return img;
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchor,
        Vector2 offset, Vector2 size, TextAnchor align, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = align;
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
        return text;
    }
}

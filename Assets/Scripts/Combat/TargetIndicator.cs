using UnityEngine;

// TargetIndicator — 目标锁定标记（头顶指示）：锁头发射时闪黄 0.8s
// 挂载：靶子/敌人（TargetDummy 自动挂；玩家不挂——自己人不需要）
public class TargetIndicator : MonoBehaviour
{
    private SpriteRenderer _marker;
    private float _lockTimer;
    private Camera _cam;
    private static Sprite _dot; // 圆点标记（代码生成）

    private void Awake()
    {
        _cam = Camera.main;
        if (_dot == null) _dot = MakeDot();
        var go = new GameObject("TargetMarker");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.4f, 0f); // 头顶
        _marker = go.AddComponent<SpriteRenderer>();
        _marker.sprite = _dot;
        _marker.color = Color.yellow;
        _marker.sortingOrder = 10;
        _marker.transform.localScale = Vector3.one * 0.5f; // 16 纹理缩到 0.5 世界（小标记）
        _marker.enabled = false;
    }

    private void Update()
    {
        _lockTimer -= Time.deltaTime;
        _marker.enabled = _lockTimer > 0f; // 锁定时显示，0.8s 后消失
        // Billboard：标记永远面向玩家（复制相机旋转——Sprite 正对相机）
        if (_marker.enabled && _cam != null)
            _marker.transform.rotation = _cam.transform.rotation;
    }

    // 锁头发射时调用：目标头顶闪黄 0.8s
    public void ShowLocked() => _lockTimer = 0.8f;

    private static Sprite MakeDot()
    {
        const int S = 16;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x - S / 2f) / (S / 2f);
                float dy = (y - S / 2f) / (S / 2f);
                px[y * S + x] = dx * dx + dy * dy <= 1f ? Color.white : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply(false);
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 16f); // PPU=16：16px=1 世界单位
    }
}

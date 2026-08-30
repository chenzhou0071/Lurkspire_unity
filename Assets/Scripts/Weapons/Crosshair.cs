using UnityEngine;
using UnityEngine.UI;

// Crosshair — 屏幕中央十字准星（运行时自动生成，原型阶段免手动搭建）
// 挂载：Main Camera（Play 时自动创建 Canvas + 横竖两条）
public class Crosshair : MonoBehaviour
{
    private void Awake()
    {
        var canvasGO = new GameObject("CrosshairCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        CreateBar("HBar", canvasGO.transform, new Vector2(12, 2));
        CreateBar("VBar", canvasGO.transform, new Vector2(2, 12));
    }

    private static void CreateBar(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = Color.white;
        var rt = img.rectTransform;
        rt.sizeDelta = size;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); // 屏幕中心
        rt.anchoredPosition = Vector2.zero;
    }
}

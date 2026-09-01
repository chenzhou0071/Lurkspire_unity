// MapBoxExporter — 地图墙盒导出：遍历场景静态 Collider → 生成 Go 墙盒配置
// 用途：Lurkspire 服务端（T5 命中判定）需要地图碰撞简化版（AABB 列表）
// 用法：菜单 Tools → Export Map Boxes → 项目根生成 MapWalls.go → 复制到 Go 仓库 internal/room/
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class MapBoxExporter : EditorWindow
{
    [MenuItem("Tools/Export Map Boxes")]
    static void Export()
    {
        var boxes = new List<Bounds>();
        var cam = Camera.main;
        foreach (var col in FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (col.isTrigger) continue;                       // 触发器不算墙
            if (col.transform.root.name.Contains("Player")) continue; // 排除玩家本体
            if (cam != null && col.transform.IsChildOf(cam.transform)) continue; // 排除相机子物体（枪/刀）
            if (col.GetComponentInParent<HealthComponent>() != null) continue;  // 排除靶子（可打击物不是墙）
            boxes.Add(col.bounds); // 世界包围盒（自动含旋转/缩放）
        }

        // 生成 Go 配置
        var sb = new StringBuilder();
        sb.AppendLine("// mapdata.go — 地图墙盒（AABB 列表）——由 Unity MapBoxExporter 导出");
        sb.AppendLine("// 用途：服务端命中判定射线求交（地图碰撞简化版）");
        sb.AppendLine("// 如需合并/简化大盒，手动编辑本文件即可");
        sb.AppendLine("package room");
        sb.AppendLine();
        sb.AppendLine("// WallBox 墙体 AABB（世界坐标）");
        sb.AppendLine("type WallBox struct { X, Y, Z, W, H, D float32 }");
        sb.AppendLine();
        sb.AppendLine("var MapWalls = []WallBox{");
        foreach (var b in boxes)
        {
            // Go 浮点字面量无 f 后缀（float32 由常量自动转换）
            sb.AppendFormat("\t{{ {0}, {1}, {2}, {3}, {4}, {5} }},\n",
                F(b.center.x), F(b.center.y), F(b.center.z),
                F(b.size.x), F(b.size.y), F(b.size.z));
        }
        sb.AppendLine("}");
        sb.AppendLine();

        string path = Path.Combine(Application.dataPath, "../MapWalls.go");
        File.WriteAllText(path, sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log($"MapBoxExporter: 导出 {boxes.Count} 个墙盒 → {path}");
    }

    static string F(float v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}

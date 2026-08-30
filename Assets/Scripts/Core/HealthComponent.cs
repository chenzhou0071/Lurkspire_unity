using UnityEngine;

// HealthComponent — Health 逻辑的 MonoBehaviour 包装（挂靶子/敌人/玩家）
// 逻辑本体是纯 C#（Health）可单测；组件负责"能被 GetComponent 找到"
public class HealthComponent : MonoBehaviour
{
    public Health Logic { get; private set; } = new Health();
}

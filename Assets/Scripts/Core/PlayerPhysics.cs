using UnityEngine;

// PlayerPhysics — 垂直物理（纯逻辑可测）：重力/跳跃初速/落地清零/下落上限
// 重力值由构造传入（PlayerController 配置）；跑墙锁高、下落加速逻辑在此
public class PlayerPhysics
{
    private readonly float _gravity;

    public PlayerPhysics(float gravity)
    {
        _gravity = gravity;
    }

    public float VerticalVelocity { get; private set; }

    // 重力步进：
    // 跑墙：保留上升惯性缓慢衰减后锁高（不下坠、不贴地飞行）
    // 空中：下落段 ×FallGravityMult 加速（速度越来越大），上限 MaxFallSpeed
    public void Tick(bool wallRunning, float dt)
    {
        if (wallRunning)
        {
            if (VerticalVelocity > 0f)
                VerticalVelocity *= 0.96f; // 上升惯性保留（跳到墙上的动势），缓慢衰减自然停住
            else
                VerticalVelocity = 0f;     // 无上升速度 → 锁高
        }
        else
        {
            float mult = VerticalVelocity < 0f ? GameConfig.FallGravityMult : 1f;
            VerticalVelocity += _gravity * mult * dt;
        }
        VerticalVelocity = Mathf.Max(VerticalVelocity, -GameConfig.MaxFallSpeed);
    }

    // 跳跃初速（按高度换算）
    public void Jump(float height)
    {
        VerticalVelocity = Mathf.Sqrt(height * -2f * _gravity);
    }

    // 落地清零垂直速度（防残留下落速度——走出台面"光速下落"根因）
    public void ResetOnLand(bool grounded)
    {
        if (grounded && VerticalVelocity < 0f) VerticalVelocity = 0f;
    }
}

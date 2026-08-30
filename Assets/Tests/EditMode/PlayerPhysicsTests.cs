using NUnit.Framework;
using UnityEngine;

// PlayerPhysicsTests — 垂直物理：重力/下落加速/上限/落地清零/跳跃初速
public class PlayerPhysicsTests
{
    private const float G = -25f;

    [Test]
    public void Gravity_Accelerates_Downward()
    {
        var p = new PlayerPhysics(G);
        p.Tick(false, 1f);
        Assert.Less(p.VerticalVelocity, 0f); // 开始下落
        Assert.AreEqual(G * 1f, p.VerticalVelocity, 0.01f);
    }

    [Test]
    public void Fall_Faster_Than_Rise()
    {
        var p = new PlayerPhysics(G);
        p.Tick(false, 0.2f); // vy = -5（进入下落段）
        Assert.Less(p.VerticalVelocity, 0f);
        float before = p.VerticalVelocity;
        p.Tick(false, 0.1f);
        // 下落段增量 = -FallGravityMult × |G| × dt（负方向加速——重力向下）
        float expected = -GameConfig.FallGravityMult * Mathf.Abs(G) * 0.1f;
        Assert.AreEqual(expected, p.VerticalVelocity - before, 0.01f);
    }

    [Test]
    public void MaxFallSpeed_Clamped()
    {
        var p = new PlayerPhysics(G);
        for (int i = 0; i < 100; i++) p.Tick(false, 1f);
        Assert.AreEqual(-GameConfig.MaxFallSpeed, p.VerticalVelocity, 0.01f); // 上限锁死
    }

    [Test]
    public void WallRun_LocksHeight()
    {
        var p = new PlayerPhysics(G);
        p.Tick(false, 1f); // 先下落
        p.Tick(true, 1f);  // 跑墙：锁高
        Assert.AreEqual(0f, p.VerticalVelocity); // 垂直速度清零（不下坠）
    }

    [Test]
    public void WallRun_KeepsRisingMomentum()
    {
        var p = new PlayerPhysics(G);
        p.Jump(3f);        // 上升中
        float before = p.VerticalVelocity;
        p.Tick(true, 0.1f); // 跑墙：保留上升惯性（缓慢衰减）
        Assert.Greater(p.VerticalVelocity, 0f);  // 还在上升
        Assert.Less(p.VerticalVelocity, before); // 但衰减了
    }

    [Test]
    public void ResetOnLand_ClearsFall()
    {
        var p = new PlayerPhysics(G);
        p.Tick(false, 1f); // 下落
        Assert.Less(p.VerticalVelocity, 0f);
        p.ResetOnLand(true); // 落地
        Assert.AreEqual(0f, p.VerticalVelocity);
    }

    [Test]
    public void Jump_Velocity_ByHeight()
    {
        var p = new PlayerPhysics(G);
        p.Jump(3f);
        Assert.AreEqual(Mathf.Sqrt(3f * -2f * G), p.VerticalVelocity, 0.01f);
    }
}

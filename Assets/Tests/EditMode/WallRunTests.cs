using NUnit.Framework;

// WallRunTests — 跑墙状态机：计时（跟随配置 WallRunSeconds）/换墙刷新/方向锁定
public class WallRunTests
{
    [Test]
    public void WallRun_Duration_ExpiresAfterConfigSeconds()
    {
        var wr = new WallRun();
        wr.Enter(new UnityEngine.Vector3(1, 0, 0), new UnityEngine.Vector3(0, 0, 1));
        Assert.IsTrue(wr.IsWallRunning);
        wr.Tick(GameConfig.WallRunSeconds - 0.5f);
        Assert.IsTrue(wr.IsWallRunning);                 // 未到时长
        wr.Tick(0.6f);                                   // 累计超过配置时长
        Assert.IsFalse(wr.IsWallRunning);
    }

    [Test]
    public void WallRun_ReEnter_Refreshes()
    {
        var wr = new WallRun();
        wr.Enter(new UnityEngine.Vector3(1, 0, 0), new UnityEngine.Vector3(0, 0, 1));
        wr.Tick(GameConfig.WallRunSeconds - 0.5f);
        wr.Enter(new UnityEngine.Vector3(0, 0, 1), new UnityEngine.Vector3(0, 0, 1)); // 换墙 → 计时刷新
        Assert.IsTrue(wr.IsWallRunning);
        wr.Tick(GameConfig.WallRunSeconds - 0.5f);
        Assert.IsTrue(wr.IsWallRunning);                 // 从换墙重新算
        wr.Tick(0.6f);
        Assert.IsFalse(wr.IsWallRunning);
    }

    [Test]
    public void RunDir_AlongWallTangent()
    {
        // 墙法线 (1,0,0) → 切线 ±(0,0,1)；上墙时输入朝 +Z → 沿 +Z 锁定
        var wr = new WallRun();
        wr.Enter(new UnityEngine.Vector3(1, 0, 0), new UnityEngine.Vector3(0, 0, 1));
        Assert.Greater(wr.RunDir.z, 0.9f);              // 沿 +Z
        Assert.Less(UnityEngine.Mathf.Abs(wr.RunDir.x), 0.1f); // 不沿法线方向
    }

    [Test]
    public void RunDir_FlipsWithInput()
    {
        var wr = new WallRun();
        wr.Enter(new UnityEngine.Vector3(1, 0, 0), new UnityEngine.Vector3(0, 0, -1)); // 上墙时输入朝 -Z
        Assert.Less(wr.RunDir.z, -0.9f); // 沿 -Z
    }

    [Test]
    public void Exit_StopsWallRun()
    {
        var wr = new WallRun();
        wr.Enter(new UnityEngine.Vector3(1, 0, 0), new UnityEngine.Vector3(0, 0, 1));
        wr.Exit();
        Assert.IsFalse(wr.IsWallRunning);
    }
}

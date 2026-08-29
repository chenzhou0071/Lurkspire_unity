using NUnit.Framework;

// WallRunTests — 跑墙状态机：3 秒计时/换墙刷新/切线方向
public class WallRunTests
{
    [Test]
    public void WallRun_Duration_ThreeSeconds()
    {
        var wr = new WallRun();
        wr.Enter(new UnityEngine.Vector3(1, 0, 0));
        Assert.IsTrue(wr.IsWallRunning);
        wr.Tick(2f);
        Assert.IsTrue(wr.IsWallRunning);
        wr.Tick(1.5f); // 累计 3.5s > 3s
        Assert.IsFalse(wr.IsWallRunning);
    }

    [Test]
    public void WallRun_ReEnter_Refreshes()
    {
        var wr = new WallRun();
        wr.Enter(new UnityEngine.Vector3(1, 0, 0));
        wr.Tick(2.5f);
        wr.Enter(new UnityEngine.Vector3(0, 0, 1)); // 换墙 → 计时刷新
        Assert.IsTrue(wr.IsWallRunning);
        wr.Tick(2f);
        Assert.IsTrue(wr.IsWallRunning); // 3s 从换墙重新算
        wr.Tick(1.5f);
        Assert.IsFalse(wr.IsWallRunning);
    }

    [Test]
    public void RunDirection_AlongWallTangent()
    {
        // 墙法线 (1,0,0) → 切线 ±(0,0,1)；输入朝 +Z → 沿 +Z
        var wr = new WallRun();
        wr.Enter(new UnityEngine.Vector3(1, 0, 0));
        var dir = wr.RunDirection(new UnityEngine.Vector3(0, 0, 1));
        Assert.Greater(dir.z, 0.9f);  // 沿 +Z
        Assert.Less(UnityEngine.Mathf.Abs(dir.x), 0.1f); // 不沿法线方向
    }

    [Test]
    public void RunDirection_FlipsWithInput()
    {
        var wr = new WallRun();
        wr.Enter(new UnityEngine.Vector3(1, 0, 0));
        var dir = wr.RunDirection(new UnityEngine.Vector3(0, 0, -1)); // 输入朝 -Z
        Assert.Less(dir.z, -0.9f); // 沿 -Z
    }

    [Test]
    public void Exit_StopsWallRun()
    {
        var wr = new WallRun();
        wr.Enter(new UnityEngine.Vector3(1, 0, 0));
        wr.Exit();
        Assert.IsFalse(wr.IsWallRunning);
    }
}

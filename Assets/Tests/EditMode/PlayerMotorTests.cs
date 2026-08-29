using NUnit.Framework;

// PlayerMotorTests — 移动逻辑：速度计算/滑铲状态机（手感数值在 T11 调，逻辑先锁）
public class PlayerMotorTests
{
    [Test]
    public void RunSpeed_MatchesConfig()
    {
        Assert.AreEqual(GameConfig.RunSpeed, PlayerMotor.ComputeSpeed(false, false));
    }

    [Test]
    public void SlideSpeed_SlightlyFaster()
    {
        float run = PlayerMotor.ComputeSpeed(false, false);
        float slide = PlayerMotor.ComputeSpeed(false, true);
        Assert.Greater(slide, run);        // 滑铲比跑步略快
        Assert.Less(slide, run * 1.5f);    // 但不能快太多
    }

    [Test]
    public void WallRun_FasterThanRun()
    {
        float run = PlayerMotor.ComputeSpeed(false, false);
        float wall = PlayerMotor.ComputeSpeed(true, false);
        Assert.Greater(wall, run);         // 跑墙是加速机动（快于跑步）
        Assert.Less(wall, run * 2f);       // 但不能离谱
    }

    [Test]
    public void Slide_EndsAfterTimeout()
    {
        var motor = new PlayerMotor();
        motor.SetSlide(true);
        Assert.IsTrue(motor.IsSliding);
        motor.TickSlide(0.5f);
        Assert.IsTrue(motor.IsSliding);
        motor.TickSlide(0.5f);             // 累计 1s > 0.8s
        Assert.IsFalse(motor.IsSliding);
    }

    [Test]
    public void Slide_ReEnter_RestartsTimer()
    {
        var motor = new PlayerMotor();
        motor.SetSlide(true);
        motor.TickSlide(0.5f);
        motor.SetSlide(false);
        motor.SetSlide(true);              // 重新滑铲 → 计时重置
        motor.TickSlide(0.7f);
        Assert.IsTrue(motor.IsSliding);    // 0.7s < 0.8s（重置后）
    }
}

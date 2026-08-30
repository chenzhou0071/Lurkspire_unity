using NUnit.Framework;

// ChargeTests — 充能锁头：10s 充能/存 3 封顶/黑白交替
public class ChargeTests
{
    [Test]
    public void Charge_Accumulates_TenSeconds()
    {
        var gun = new GunController();
        gun.Tick(10f);
        Assert.AreEqual(1, gun.Charge);
    }

    [Test]
    public void Charge_Max_Three()
    {
        var gun = new GunController();
        for (int i = 0; i < 5; i++) gun.Tick(10f);
        Assert.AreEqual(GameConfig.ChargeMax, gun.Charge); // 存 3 发封顶
    }

    [Test]
    public void LockOn_Consumes_AndAlternates()
    {
        var gun = new GunController();
        gun.Tick(10f);
        gun.TryLockOn(out int first);
        Assert.AreEqual(0, gun.Charge);   // 消耗一发
        gun.Tick(10f);
        gun.TryLockOn(out int second);
        Assert.AreEqual(0, first);        // 第 1 次白枪
        Assert.AreEqual(1, second);       // 第 2 次黑枪
    }

    [Test]
    public void LockOn_NoCharge_Fails()
    {
        var gun = new GunController();
        Assert.IsFalse(gun.TryLockOn(out _)); // 无充能
    }
}

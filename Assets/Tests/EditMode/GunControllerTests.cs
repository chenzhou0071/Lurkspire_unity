using NUnit.Framework;

// GunControllerTests — 双枪逻辑：交替序列/弹匣/冷却
public class GunControllerTests
{
    [Test]
    public void Fire_AlternatesBlackWhite()
    {
        var gun = new GunController();
        gun.TryFire(out int first);
        gun.Tick(GameConfig.FireInterval + 0.01f); // 模拟射速间隔
        gun.TryFire(out int second);
        gun.Tick(GameConfig.FireInterval + 0.01f);
        gun.TryFire(out int third);
        Assert.AreEqual(0, first);   // 黑枪
        Assert.AreEqual(1, second);  // 白枪
        Assert.AreEqual(0, third);   // 又黑
    }

    [Test]
    public void Magazine_Empty_BlocksFire()
    {
        var gun = new GunController();
        for (int i = 0; i < GameConfig.MagazineSize; i++) gun.TryFire(out _);
        Assert.IsFalse(gun.TryFire(out _)); // 弹匣空
        gun.Reload();
        Assert.IsFalse(gun.TryFire(out _)); // 换弹中不能开火
        gun.Tick(GameConfig.ReloadSeconds + 0.01f); // 换弹完成
        Assert.IsTrue(gun.TryFire(out _));  // 换弹后可开火
    }

    [Test]
    public void Reload_RequiresTime_AndBlocksFire()
    {
        var gun = new GunController();
        for (int i = 0; i < GameConfig.MagazineSize; i++) gun.TryFire(out _);
        gun.Reload();
        Assert.IsTrue(gun.IsReloading);
        gun.Tick(GameConfig.ReloadSeconds * 0.5f);
        Assert.IsTrue(gun.IsReloading);              // 一半时间还没好
        Assert.IsFalse(gun.TryFire(out _));          // 期间不能开火
        gun.Tick(GameConfig.ReloadSeconds * 0.6f);   // 累计超过 1.2s
        Assert.IsFalse(gun.IsReloading);
        Assert.AreEqual(GameConfig.MagazineSize, gun.Ammo); // 补满
    }

    [Test]
    public void Cooldown_BlocksInstantFire()
    {
        var gun = new GunController();
        gun.TryFire(out _);
        Assert.IsFalse(gun.TryFire(out _)); // 冷却未到
        gun.Tick(GameConfig.FireInterval + 0.01f);
        Assert.IsTrue(gun.TryFire(out _));  // 冷却结束
    }

    [Test]
    public void Fire_ConsumesAmmo()
    {
        var gun = new GunController();
        int start = gun.Ammo;
        gun.TryFire(out _);
        Assert.AreEqual(start - 1, gun.Ammo);
    }
}

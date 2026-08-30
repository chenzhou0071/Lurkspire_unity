using NUnit.Framework;

// DamageSystemTests — 血量：4 枪击杀/2 刀击杀/格挡介入
public class DamageSystemTests
{
    [Test]
    public void FourGunShots_Kill()
    {
        var health = new Health();
        for (int i = 0; i < 4; i++) health.TakeDamage(GameConfig.GunDamage, null);
        Assert.IsTrue(health.IsDead);
        Assert.AreEqual(0, health.HP);
    }

    [Test]
    public void TwoSwordHits_Kill()
    {
        var health = new Health();
        health.TakeDamage(GameConfig.SwordDamage, null);
        Assert.IsFalse(health.IsDead); // 一刀 50 不死
        health.TakeDamage(GameConfig.SwordDamage, null);
        Assert.IsTrue(health.IsDead);  // 两刀死
    }

    [Test]
    public void Blocking_ReducesDamage()
    {
        var health = new Health();
        var sword = new SwordController();
        sword.IsBlocking = true;
        health.TakeDamage(25, sword);
        Assert.AreEqual(100 - 12, health.HP); // 25 → 12（50% 减伤向下取整）
        Assert.AreEqual(90, sword.Block);     // 条 -10
    }

    [Test]
    public void Death_FiresOnce()
    {
        var health = new Health();
        int deaths = 0;
        health.OnDeath += () => deaths++;
        health.TakeDamage(100, null);
        health.TakeDamage(100, null); // 已死：忽略
        Assert.AreEqual(1, deaths);
    }

    [Test]
    public void Reset_Restores()
    {
        var health = new Health();
        health.TakeDamage(50, null);
        health.Reset();
        Assert.AreEqual(GameConfig.MaxHealth, health.HP);
        Assert.IsFalse(health.IsDead);
    }
}

using NUnit.Framework;

// GameConfigTests — 设计数值锁定（改数值必须改这里 + GameConfig 双处，防止误调）
public class GameConfigTests
{
    [Test]
    public void WeaponNumbers_AsDesigned()
    {
        Assert.AreEqual(4, GameConfig.HitsToKill);          // 双枪 4 发
        Assert.AreEqual(25, GameConfig.GunDamage);          // 每发 25% = 100/4
        Assert.AreEqual(3, GameConfig.ChargeMax);           // 锁头存 3 发
        Assert.AreEqual(10f, GameConfig.ChargeSeconds);     // 10s 充能
        Assert.AreEqual(50, GameConfig.SwordDamage);        // 刀 50% = 两刀
        Assert.AreEqual(100, GameConfig.BlockMax);          // 格挡条
        Assert.AreEqual(10, GameConfig.BlockCostPerShot);   // 挡一枪 -10
        Assert.AreEqual(5f, GameConfig.BlockRegenPerSec);   // 回 5/s
        Assert.AreEqual(0.5f, GameConfig.BlockDamageMult);  // 减伤 50%
        Assert.AreEqual(20, GameConfig.BlockGainOnHit);     // 砍人 +20
        Assert.AreEqual(3f, GameConfig.WallRunSeconds);     // 跑墙 3s
        Assert.AreEqual(100, GameConfig.MaxHealth);         // 血量
        Assert.AreEqual(1f, GameConfig.SpawnInvulnSeconds); // 重生无敌
    }
}

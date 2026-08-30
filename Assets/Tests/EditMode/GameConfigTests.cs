using NUnit.Framework;

// GameConfigTests — 设计数值锁定（改数值必须改这里 + GameConfig 双处，防止误调）
public class GameConfigTests
{
    [Test]
    public void WeaponNumbers_AsDesigned()
    {
        Assert.AreEqual(4, GameConfig.HitsToKill);          // 双枪 4 发
        Assert.AreEqual(25, GameConfig.GunDamage);          // 每发 25% = 100/4
        Assert.AreEqual(50, GameConfig.LockOnDamage);       // 锁头伤害
        Assert.AreEqual(3, GameConfig.ChargeMax);           // 锁头存 3 发
        Assert.AreEqual(10f, GameConfig.ChargeSeconds);     // 10s 充能
        Assert.AreEqual(0.6f, GameConfig.LockSpinSeconds);  // 锁头转枪蓄力时长
        Assert.AreEqual(50, GameConfig.SwordDamage);        // 刀 50% = 两刀
        Assert.AreEqual(100, GameConfig.BlockMax);          // 格挡条
        Assert.AreEqual(10, GameConfig.BlockCostPerShot);   // 挡一枪 -10
        Assert.AreEqual(5f, GameConfig.BlockRegenPerSec);   // 回 5/s
        Assert.AreEqual(0.5f, GameConfig.BlockDamageMult);  // 减伤 50%
        Assert.AreEqual(20, GameConfig.BlockGainOnHit);     // 砍人 +20
        Assert.AreEqual(1f, GameConfig.WallRunSeconds);     // 跑墙 1s（超时掉墙）
        Assert.AreEqual(2.2f, GameConfig.WallJumpHeight);   // 跑墙跳高度（略低于普通跳）
        Assert.AreEqual(2.5f, GameConfig.AirJumpHeight);    // 二段跳高度
        Assert.AreEqual(14f, GameConfig.AirJumpSpeed);      // 二段跳变向速度
        Assert.AreEqual(100, GameConfig.MaxHealth);         // 血量
        Assert.AreEqual(1f, GameConfig.SpawnInvulnSeconds); // 重生无敌
        Assert.AreEqual(15f, GameConfig.WallJumpAwaySpeed); // 跑墙侧跳速度（弹射）
        Assert.AreEqual(18f, GameConfig.WallJumpForwardSpeed); // 跑墙左前飞速度
        Assert.AreEqual(0.3f, GameConfig.AirControlWeight); // 空中操控权重
        Assert.AreEqual(1.6f, GameConfig.FallGravityMult); // 下落段重力倍率
        Assert.AreEqual(60f, GameConfig.MaxFallSpeed);       // 下落速度上限
    }
}

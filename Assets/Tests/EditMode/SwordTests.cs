using NUnit.Framework;

// SwordTests — 武士刀逻辑：格挡条数学/挥砍冷却/冲刺斩冷却
public class SwordTests
{
    [Test]
    public void Block_DamageReduced_FiftyPercent()
    {
        var sword = new SwordController();
        sword.Block = 100;
        sword.IsBlocking = true;
        int dmg = 25;
        sword.BlockDamage(ref dmg);
        Assert.AreEqual(12, dmg);      // 25 × 0.5 = 12（向下取整）
        Assert.AreEqual(90, sword.Block); // 挡一枪 -10
    }

    [Test]
    public void Block_Regen_FivePerSec()
    {
        var sword = new SwordController();
        sword.Block = 50;
        sword.Tick(2f);
        Assert.AreEqual(60, sword.Block);
    }

    [Test]
    public void Hit_Enemy_GainsTwenty()
    {
        var sword = new SwordController();
        sword.Block = 10;
        sword.GainBlock(GameConfig.BlockGainOnHit);
        Assert.AreEqual(30, sword.Block);
    }

    [Test]
    public void Block_Empty_NoReduction()
    {
        var sword = new SwordController();
        sword.Block = 5; // 不够 10
        sword.IsBlocking = true;
        int dmg = 25;
        sword.BlockDamage(ref dmg);
        Assert.AreEqual(25, dmg);  // 不触发减伤
        Assert.AreEqual(5, sword.Block);
    }

    [Test]
    public void Swing_Cooldown_Blocks()
    {
        var sword = new SwordController();
        sword.TrySwing();
        Assert.IsFalse(sword.TrySwing()); // 冷却中
        sword.Tick(GameConfig.SwordArcSeconds + 0.01f);
        Assert.IsTrue(sword.TrySwing());
    }

    [Test]
    public void DashAttack_Cooldown()
    {
        var sword = new SwordController();
        sword.TryDashAttack();
        Assert.IsFalse(sword.TryDashAttack()); // 冷却中
        sword.Tick(GameConfig.DashAttackCooldown + 0.01f);
        Assert.IsTrue(sword.TryDashAttack());
    }
}

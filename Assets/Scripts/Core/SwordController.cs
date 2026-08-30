using UnityEngine;

// SwordController — 武士刀逻辑（纯逻辑可测）：挥砍冷却/冲刺斩冷却/格挡条
// 数值：两刀击杀 50×2、格挡条 100/挡枪 -10/回 5/s/减伤 50%/砍人 +20
public class SwordController
{
    private float _swingCooldown;
    private float _dashCooldown;
    public bool IsBlocking { get; set; }
    public float Block { get; set; } = GameConfig.BlockMax;

    // 挥砍（左键）：冷却中失败
    public bool TrySwing()
    {
        if (_swingCooldown > 0f) return false;
        _swingCooldown = GameConfig.SwordArcSeconds;
        return true;
    }

    // 冲刺斩（Shift）：位移 + 一刀伤害，冷却 1.5s
    public bool TryDashAttack()
    {
        if (_dashCooldown > 0f) return false;
        _dashCooldown = GameConfig.DashAttackCooldown;
        return true;
    }

    // 冲刺冷却剩余（HUD 显示用）
    public float DashCooldownRemain => _dashCooldown;

    // 格挡伤害：条够 10 则减伤 50%（返回是否成功格挡）
    public bool BlockDamage(ref int damage)
    {
        if (!IsBlocking || Block < GameConfig.BlockCostPerShot) return false;
        Block -= GameConfig.BlockCostPerShot;
        damage = Mathf.FloorToInt(damage * GameConfig.BlockDamageMult);
        return true;
    }

    // 砍中敌人：格挡条 +20（以战养战）
    public void GainBlock(int amount) =>
        Block = Mathf.Min(GameConfig.BlockMax, Block + amount);

    // 每帧：冷却倒计时 + 格挡条恢复（5/s）
    public void Tick(float dt)
    {
        if (_swingCooldown > 0f) _swingCooldown -= dt;
        if (_dashCooldown > 0f) _dashCooldown -= dt;
        if (Block < GameConfig.BlockMax)
            Block = Mathf.Min(GameConfig.BlockMax, Block + GameConfig.BlockRegenPerSec * dt);
    }
}

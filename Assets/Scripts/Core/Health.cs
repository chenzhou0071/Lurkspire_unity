using System;

// Health — 血量（纯逻辑可测）：伤害经格挡介入后扣血、死亡回调、重置
public class Health
{
    public int HP { get; private set; } = GameConfig.MaxHealth;
    public bool IsDead => HP <= 0;
    public event Action OnDeath;

    // 伤害：先过格挡（目标持刀格挡时减伤），再扣血
    public void TakeDamage(int damage, SwordController blocker)
    {
        if (IsDead) return;
        if (blocker != null) blocker.BlockDamage(ref damage);
        HP -= damage;
        if (HP <= 0)
        {
            HP = 0;
            OnDeath?.Invoke();
        }
    }

    public void Reset() => HP = GameConfig.MaxHealth;
}

// DamageSystem — 命中入口（静态：射线/范围命中后调用）
public static class DamageSystem
{
    public static void ApplyHit(Health target, int damage, SwordController blocker)
        => target.TakeDamage(damage, blocker);
}

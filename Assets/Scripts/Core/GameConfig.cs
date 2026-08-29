// GameConfig — 所有玩法数值的唯一事实源（调手感只改这里）
// 注意：改数值必须同步改 GameConfigTests，防止误调
public static class GameConfig
{
    // ---- 双枪（左手黑 / 右手白） ----
    public const int HitsToKill = 4;           // 4 发击杀
    public const int GunDamage = 25;           // 每发 25% = 100/4
    public const int ChargeMax = 3;            // 锁头充能上限
    public const float ChargeSeconds = 10f;    // 充能一发时间
    public const int MagazineSize = 24;        // 弹匣 24 发
    public const float ReloadSeconds = 1.2f;   // 换弹时间
    public const float FireInterval = 0.09f;   // 交替射速间隔

    // ---- 武士刀 ----
    public const int SwordDamage = 50;         // 两刀击杀
    public const float SwordRange = 3f;        // 近战距离
    public const float SwordArcSeconds = 0.25f; // 挥砍前摇/冷却
    public const float DashAttackRange = 6f;   // 冲刺斩距离
    public const float DashAttackCooldown = 1.5f;

    // ---- 格挡 ----
    public const int BlockMax = 100;           // 格挡条上限
    public const int BlockCostPerShot = 10;    // 挡一枪 -10（可挡 10 枪）
    public const float BlockRegenPerSec = 5f;  // 每秒回 5（20 秒回满）
    public const float BlockDamageMult = 0.5f; // 格挡减伤 50%
    public const int BlockGainOnHit = 20;      // 刀命中敌人 +20（抵两枪）

    // ---- 机动 ----
    public const float RunSpeed = 12f;         // 快跑速度（手感阶段可调）
    public const float WallRunSeconds = 3f;    // 跑墙时长（换墙/落地刷新）
    public const float WallRunSpeedMult = 0.85f; // 跑墙沿墙速度倍率
    public const float SlideSpeedMult = 1.1f;  // 滑铲速度倍率（略快于跑步）
    public const float SlideDuration = 0.8f;   // 滑铲时长
    public const float JumpHeight = 2f;        // 跳跃高度（非核心）

    // ---- 对局 ----
    public const int MaxHealth = 100;          // 血量
    public const float SpawnInvulnSeconds = 1f; // 重生无敌
    public const float RespawnSeconds = 2f;    // 死亡到重生
}

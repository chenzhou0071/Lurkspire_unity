// GameConfig — 所有玩法数值的唯一事实源（调手感只改这里）
// 注意：改数值必须同步改 GameConfigTests，防止误调
public static class GameConfig
{
    // ---- 双枪（左手黑 / 右手白） ----
    public const int HitsToKill = 4;           // 4 发击杀
    public const int GunDamage = 25;           // 每发 25% = 100/4
    public const int LockOnDamage = 50;        // 锁头伤害（必中一击，两发威力）
    public const int ChargeMax = 3;            // 锁头充能上限
    public const float ChargeSeconds = 10f;    // 充能一发时间
    public const float LockSpinSeconds = 1f;   // 锁头转枪蓄力时长（360° 由快变慢）
    public const int MagazineSize = 8;         // 弹匣 8 发
    public const float ReloadSeconds = 1.2f;   // 换弹时间
    public const float FireInterval = 0.12f;   // 交替射速间隔

    // ---- 武士刀 ----
    public const int SwordDamage = 50;         // 两刀击杀
    public const float SwordRange = 3f;        // 近战距离
    public const float SwordArcSeconds = 0.25f; // 挥砍前摇/冷却
    public const float DashAttackRange = 6f;   // 冲刺斩距离
    public const float DashAttackCooldown = 1.5f;
    public const float DashAttackDuration = 0.2f; // 冲刺斩时长（平滑冲刺，非瞬移）

    // ---- 格挡 ----
    public const int BlockMax = 100;           // 格挡条上限
    public const int BlockCostPerShot = 10;    // 挡一枪 -10（可挡 10 枪）
    public const float BlockRegenPerSec = 5f;  // 每秒回 5（20 秒回满）
    public const float BlockDamageMult = 0.5f; // 格挡减伤 50%
    public const int BlockGainOnHit = 20;      // 刀命中敌人 +20（抵两枪）

    // ---- 机动 ----
    public const float RunSpeed = 16f;         // 快跑速度（手感阶段可调）
    public const float WallRunSeconds = 1.5f;  // 跑墙时长（超时掉墙；换墙/落地刷新）
    public const float WallRunSpeedMult = 1.6f; // 跑墙沿墙速度倍率（快于跑步，加速机动）
    public const float SlideSpeedMult = 1.3f;  // 滑铲速度倍率（比跑步快）
    public const float SlideDuration = 0.8f;   // 滑铲时长
    public const float JumpHeight = 3f;        // 跳跃高度
    public const float AirJumpHeight = 2.5f;   // 二段跳高度（空中变向修正，略低于普通跳）
    public const float AirJumpSpeed = 14f;     // 二段跳变向速度（= 跑步速度，够转向/扑墙）
    public const float WallJumpHeight = 2.2f;  // 跑墙跳高度（略低——重点是横向弹射）
    public const float WallJumpAwaySpeed = 15f; // 跑墙跳：侧跳（离墙）速度——强弹射
    public const float WallJumpForwardSpeed = 18f; // 跑墙跳：左前飞（W 修饰）速度（合速度需快于跑墙）
    public const float AirControlWeight = 0.3f; // 空中操控权重（跳出后输入影响 30%，动量保持 70%）
    public const float FallGravityMult = 1.6f;  // 下落段重力倍率（下落速度越来越大，落地干脆）
    public const float MaxFallSpeed = 60f;     // 下落速度上限（防高空瞬移穿透）

    // ---- 对局 ----
    public const int MaxHealth = 100;          // 血量
    public const float SpawnInvulnSeconds = 1f; // 重生无敌
    public const float RespawnSeconds = 2f;    // 死亡到重生
}

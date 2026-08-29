// PlayerMotor — 角色移动核心（纯逻辑可测）：速度计算/滑铲状态
// 放在 Core 目录（Lurkspire.Core 程序集）以便 EditMode 测试
public class PlayerMotor
{
    public const float SlideDuration = 0.8f;
    private float _slideTimer;
    public bool IsSliding { get; private set; }

    // 速度计算：跑墙限速、滑铲略快
    public static float ComputeSpeed(bool wallRunning, bool sliding)
    {
        float speed = GameConfig.RunSpeed;
        if (wallRunning) speed *= GameConfig.WallRunSpeedMult;
        if (sliding) speed *= GameConfig.SlideSpeedMult;
        return speed;
    }

    // 滑铲状态机：启动计时，到时自动结束
    public void SetSlide(bool slide)
    {
        if (slide && !IsSliding) _slideTimer = SlideDuration;
        IsSliding = slide;
    }

    public void TickSlide(float dt)
    {
        if (!IsSliding) return;
        _slideTimer -= dt;
        if (_slideTimer <= 0f) IsSliding = false;
    }
}

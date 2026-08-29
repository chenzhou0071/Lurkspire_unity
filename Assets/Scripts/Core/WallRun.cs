using UnityEngine;

// WallRun — 跑墙状态机（纯逻辑可测）：3 秒计时，换墙/落地刷新
public class WallRun
{
    private float _timer;
    private Vector3 _wallNormal;
    public bool IsWallRunning { get; private set; }

    // 上墙（换墙自动刷新计时）
    public void Enter(Vector3 wallNormal)
    {
        _wallNormal = wallNormal;
        _timer = GameConfig.WallRunSeconds;
        IsWallRunning = true;
    }

    public void Exit() => IsWallRunning = false;

    // 每秒调用：计时耗尽自动掉墙
    public void Tick(float dt)
    {
        if (!IsWallRunning) return;
        _timer -= dt;
        if (_timer <= 0f) IsWallRunning = false;
    }

    // 跑墙方向：墙面切线（法线 × 上方向），跟随输入选朝向
    public Vector3 RunDirection(Vector3 inputDir)
    {
        Vector3 tangent = Vector3.Cross(Vector3.up, _wallNormal).normalized;
        return Vector3.Dot(inputDir, tangent) >= 0 ? tangent : -tangent;
    }
}

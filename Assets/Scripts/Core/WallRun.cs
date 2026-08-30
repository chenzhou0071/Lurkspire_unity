using UnityEngine;

// WallRun — 跑墙状态机（纯逻辑可测）：计时/换墙刷新/方向锁定
public class WallRun
{
    private float _timer;
    private Vector3 _wallNormal;
    public bool IsWallRunning { get; private set; }
    public Vector3 RunDir { get; private set; } // 上墙时锁定的跑墙方向（防视角转向导致方向翻转）

    // 上墙（换墙自动刷新计时；方向按上墙瞬间的输入锁定）
    public void Enter(Vector3 wallNormal, Vector3 inputDir)
    {
        _wallNormal = wallNormal;
        _timer = GameConfig.WallRunSeconds;
        RunDir = ComputeRunDirection(inputDir);
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

    // 跑墙方向：墙面切线（法线 × 上方向），跟随上墙时的输入选朝向
    private Vector3 ComputeRunDirection(Vector3 inputDir)
    {
        Vector3 tangent = Vector3.Cross(Vector3.up, _wallNormal).normalized;
        return Vector3.Dot(inputDir, tangent) >= 0 ? tangent : -tangent;
    }
}

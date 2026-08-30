using UnityEngine;

// PlayerController — 状态协调：跑墙/滑铲/移动方向/二段跳/倾斜/调试
// 依赖：PlayerInput（输入）/PlayerPhysics（垂直物理）/PlayerMotor（速度）/WallRun（跑墙）
// 由 PlayerInput.Awake 自动挂载（场景零操作）；重力数值在 Inspector 可调
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float wallDetectRange = 1.2f; // 跑墙检测距离（左右侧）
    [SerializeField] private PlayerCamera fpsCamera; // 第一人称相机（倾斜转发；不拖则无倾斜）
    [SerializeField] private float debugSpeed;   // Inspector 调试：当前水平速度
    [SerializeField] private string debugState;  // Inspector 调试：当前状态

    private CharacterController _cc;
    private PlayerInput _input;
    private PlayerPhysics _physics;
    private PlayerMotor _motor;
    private WallRun _wallRun;
    private Vector3 _velocity;
    private Vector3 _lastWallNormal;
    private Vector3 _jumpMomentum; // 跳出的横向动量（落地清零，防止被方向覆盖）
    private float _wallJumpCooldown; // 跳出冷却：期间禁止上任何墙（防跳出瞬间吸回墙）
    private bool _airJumpReady = true; // 二段跳可用（落地/上墙刷新；空中用一次）
    private bool _wallJumpActive;    // 跑墙跳状态：只有它触发同墙防吸回（二段跳可自由上墙）
    private bool _prevWallRunning;   // 上一帧跑墙状态（检测"刚掉墙"）
    private float _wallDropCooldown; // 掉墙冷却：超时掉墙后 0.5s 内禁上墙（防"掉-吸"循环）

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInput>();
        if (_input == null) _input = gameObject.AddComponent<PlayerInput>(); // 兜底
        _physics = new PlayerPhysics(gravity);
        _motor = new PlayerMotor();
        _wallRun = new WallRun();
    }

    private void Update()
    {
        var move = _input.Move;
        bool jumpPressed = _input.JumpPressed;
        bool slidePressed = _input.SlidePressed;
        bool wPressed = _input.WPressed;
        float dt = Time.deltaTime;

        // ---- 滑铲（C 键点按启动，仅落地可用、跑墙中禁止——防空中/跑墙边界冲突） ----
        // 落地判断用射线兜底（_cc.isGrounded 落地滞后——会导致落地后按 C 无反应）
        if (slidePressed && (_cc.isGrounded || Physics.Raycast(transform.position, Vector3.down, 0.25f))
            && !_wallRun.IsWallRunning)
            _motor.SetSlide(true);
        _motor.TickSlide(dt);
        _cc.height = Mathf.Lerp(_cc.height, _motor.IsSliding ? 0.6f : 2f, 12f * dt);
        _cc.center = new Vector3(0, _cc.height / 2f, 0);
        // 视觉压扁（缩放根物体——原型阶段不做底部补偿，换正式建模后按模型调）
        float targetScaleY = _motor.IsSliding ? 0.7f : 1f;
        float sy = Mathf.Lerp(transform.localScale.y, targetScaleY, 16f * dt);
        transform.localScale = new Vector3(1f, sy, 1f);

        // ---- 跑墙检测（空中 + 左右侧有墙 + 按 W → 上墙） ----
        // 地面检测：CC.isGrounded 静止时会抖动（皮肤宽度/浮点）——加脚下射线兜底，状态稳定
        bool grounded = _cc.isGrounded
            || Physics.Raycast(transform.position, Vector3.down, 0.25f);
        _wallJumpCooldown -= dt;
        bool wallHit = Physics.Raycast(transform.position, transform.right, out var hit, wallDetectRange);
        if (!wallHit) // 右侧没墙 → 检测左侧
            wallHit = Physics.Raycast(transform.position, -transform.right, out hit, wallDetectRange);
        // 下落中（vy<0）不触发跑墙——防止从高处跳下时被墙吸住（跳不下去）
        bool rising = _physics.VerticalVelocity >= 0f;
        if (!grounded && wallHit && wPressed && !_wallRun.IsWallRunning
            && _wallJumpCooldown <= 0f && _wallDropCooldown <= 0f && rising)
        {
            bool sameWall = _wallJumpActive && _jumpMomentum.sqrMagnitude > 0f
                && Vector3.Dot(hit.normal, _lastWallNormal) > 0.7f; // 仅跑墙跳防吸回原墙
            if (!sameWall)
            {
                _wallRun.Enter(hit.normal, transform.forward); // 上墙：锁定跑墙方向（防视角转向翻转）
                _lastWallNormal = hit.normal;
                _airJumpReady = true; // 上墙刷新二段跳
            }
        }
        _wallRun.Tick(dt);
        bool wallRunning = _wallRun.IsWallRunning;
        // 跑墙中墙没了（跑到尽头）→ 自动退出跑墙（惯性飞出，恢复正常物理）
        if (wallRunning)
        {
            bool stillWall = Physics.Raycast(transform.position, transform.right, wallDetectRange)
                || Physics.Raycast(transform.position, -transform.right, wallDetectRange);
            if (!stillWall)
            {
                _wallRun.Exit();
                wallRunning = false;
            }
        }
        // 超时掉墙（上一帧跑墙 → 这一帧掉且没落地）→ 掉墙冷却（防立刻吸回循环）
        if (_prevWallRunning && !wallRunning && !grounded)
            _wallDropCooldown = 0.5f;
        _prevWallRunning = wallRunning;
        _wallDropCooldown -= dt;
        if (grounded && wallRunning)
            _wallRun.Exit(); // 落地退出（下次上墙自动刷新 3 秒）
        _physics.ResetOnLand(grounded); // 落地清零垂直速度（防残留下落速度——走出台面"光速下落"根因）
        if (wallRunning && _motor.IsSliding)
            _motor.SetSlide(false); // 上墙时强制退出滑铲（两状态不共存）

        // ---- 移动方向（跑墙：只有 W 沿墙切线前进；A/D/S 无效） ----
        // 跳出的动量优先保持，空中可混合操控微调落点（动量 0.7 + 输入 0.3）
        if (_jumpMomentum.sqrMagnitude > 0f)
        {
            Vector3 inputDir = (transform.right * move.x + transform.forward * move.y).normalized;
            float keep = 1f - GameConfig.AirControlWeight;
            _velocity.x = _jumpMomentum.x * keep + inputDir.x * GameConfig.RunSpeed * GameConfig.AirControlWeight;
            _velocity.z = _jumpMomentum.z * keep + inputDir.z * GameConfig.RunSpeed * GameConfig.AirControlWeight;
            if (grounded) _jumpMomentum = Vector3.zero; // 落地动量清零
            if (grounded) _wallJumpActive = false;     // 落地解除防吸回
        }
        else
        {
            float speed = PlayerMotor.ComputeSpeed(wallRunning, _motor.IsSliding);
            Vector3 dir;
            if (wallRunning)
                dir = wPressed ? _wallRun.RunDir : Vector3.zero; // 固定方向（上墙时锁定）
            else
                dir = (transform.right * move.x + transform.forward * move.y).normalized;
            _velocity.x = dir.x * speed;
            _velocity.z = dir.z * speed;
        }

        // ---- 跑墙倾斜：按墙相对玩家的左右侧（法线·right > 0 = 墙在右 → 左倾） ----
        if (fpsCamera != null)
        {
            float side = Vector3.Dot(_lastWallNormal, transform.right);
            fpsCamera.SetTilt(wallRunning ? (side >= 0f ? -12f : 12f) : 0f);
        }

        // ---- 重力（PlayerPhysics：跑墙锁高/下落加速/上限） ----
        _physics.Tick(wallRunning, dt);
        _velocity.y = _physics.VerticalVelocity;

        // ---- 跳（空格：跑墙跳 / 落地起跳 / 空中二段跳（变向）） ----
        if (jumpPressed)
        {
            if (wallRunning)
            {
                // 跑墙跳：默认侧跳（离墙+蹬墙跳）；按住 W 加沿墙前冲（左前飞——沿锁定方向）
                Vector3 jumpVel = _lastWallNormal * GameConfig.WallJumpAwaySpeed;
                if (wPressed)
                    jumpVel += _wallRun.RunDir * GameConfig.WallJumpForwardSpeed;
                _physics.Jump(GameConfig.WallJumpHeight); // 垂直同步 PlayerPhysics（防下一帧覆盖）
                jumpVel.y = _physics.VerticalVelocity;
                _velocity = jumpVel;
                _jumpMomentum = jumpVel; _jumpMomentum.y = 0f; // 记横向动量（落地清零）
                _wallJumpCooldown = 0.2f; // 跳出冷却：0.2s 内禁止上任何墙（防瞬间吸回）
                _wallJumpActive = true;   // 跑墙跳：激活同墙防吸回
                _wallRun.Exit(); // 跳离墙面（重新上墙刷新计时）
            }
            else if (grounded)
            {
                _physics.Jump(GameConfig.JumpHeight);
                _airJumpReady = true; // 落地起跳刷新二段跳
            }
            else if (_airJumpReady)
            {
                // 二段跳：向输入方向变向（无输入则保持当前水平速度，垂直跳）
                Vector3 inputDir = (transform.right * move.x + transform.forward * move.y).normalized;
                Vector3 jumpVel;
                if (inputDir.sqrMagnitude > 0f)
                    jumpVel = inputDir * GameConfig.AirJumpSpeed; // 变向冲
                else
                    jumpVel = new Vector3(_velocity.x, 0f, _velocity.z); // 保持动量垂直跳
                _physics.Jump(GameConfig.AirJumpHeight); // 垂直同步 PlayerPhysics（防下一帧覆盖）
                jumpVel.y = _physics.VerticalVelocity;
                _velocity = jumpVel;
                _jumpMomentum = jumpVel; _jumpMomentum.y = 0f; // 复用空中操控（可微调）
                _airJumpReady = false; // 空中二段跳用完（落地/上墙刷新）
                _wallJumpActive = false; // 二段跳非墙跳：可自由上墙（解除防吸回）
            }
        }

        // ---- 调试信息（Inspector 实时显示，Play 模式下可见） ----
        debugSpeed = new Vector2(_velocity.x, _velocity.z).magnitude;
        debugState = wallRunning ? "WALLRUN"
            : _motor.IsSliding ? "SLIDE"
            : grounded ? "GROUND"
            : _jumpMomentum.sqrMagnitude > 0f ? "AIR(MOMENTUM)" : "AIR";

        _cc.Move(_velocity * dt);
    }
}

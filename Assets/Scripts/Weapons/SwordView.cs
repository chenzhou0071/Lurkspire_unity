using UnityEngine;
using UnityEngine.InputSystem;

// SwordView — 武士刀表现层：左键下劈/Shift 平滑冲刺斩/右键格挡（横刀姿态）
// 伤害目标（靶子/敌人）T8 接入——先做输入/冷却/姿态动画
// 挂载：玩家物体；刀视觉子物体（细长 Cube，相机下）拖到 Blade 槽
public class SwordView : MonoBehaviour
{
    [SerializeField] private Transform blade; // 刀视觉（细长 Cube，相机子物体）
    [SerializeField] private float swingPitchUp = 50f;   // 挥砍起始（刀尖朝上）
    [SerializeField] private float swingPitchDown = -40f; // 挥砍结束（劈下）
    private SwordController _sword = new SwordController();
    private CharacterController _cc;
    private Collider[] _myColliders; // 玩家全部碰撞体（范围伤害全部忽略——防砍自己扣血）
    private Vector3 _bladeBaseScale; // 刀基础 Scale（Awake 记录，防覆盖手动设置）
    private float _swingAnim = 1f;   // 挥砍动画进度（1=空闲）
    private bool _dashing;           // 冲刺中（平滑位移）
    private float _dashT;
    private Vector3 _dashDir;
    private bool _dashRecoil;        // 冲刺结束回弹（横劈一下再回正）
    private float _dashRecoilT;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _myColliders = GetComponentsInChildren<Collider>(); // CC + 子物体全部
        if (blade != null) _bladeBaseScale = blade.localScale;
    }

    private void Update()
    {
        _sword.Tick(Time.deltaTime);
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // 左键挥砍（下劈动画 + 前方 3m 范围伤害）
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && _sword.TrySwing())
        {
            _swingAnim = 0f; // 开始挥砍动画
            SwingHit();
        }
        // Shift 冲刺斩：平滑冲刺（0.2s 冲 6m）
        if (kb != null && kb.leftShiftKey.wasPressedThisFrame && _sword.TryDashAttack())
            StartDash();
        // 右键格挡（按住维持；格挡减伤在伤害接入时生效）
        _sword.IsBlocking = mouse != null && mouse.rightButton.isPressed;

        // ---- 冲刺位移（平滑，非瞬移） ----
        if (_dashing)
        {
            _dashT += Time.deltaTime / GameConfig.DashAttackDuration;
            _cc.Move(_dashDir * (GameConfig.DashAttackRange / GameConfig.DashAttackDuration) * Time.deltaTime);
            if (_dashT >= 1f)
            {
                _dashing = false;
                _dashRecoil = true;  // 冲刺结束：横劈回弹
                _dashRecoilT = 0f;
                _swingAnim = 1f;     // 取消冲刺触发的挥砍动画（只留横劈回弹，不接普通下劈）
                RestoreCollisions(); // 恢复与目标的碰撞
            }
        }

        // ---- 刀姿态动画（优先级：冲刺横刀 > 回弹横劈 > 挥砍下劈 > 格挡 > 默认持刀） ----
        if (blade != null)
        {
            if (_dashing)
            {
                // 冲刺姿态：刀横置（绕 Z 转 90°，横刀冲刺）
                blade.localRotation = Quaternion.Slerp(blade.localRotation,
                    Quaternion.Euler(0f, 0f, 90f), 16f * Time.deltaTime);
            }
            else if (_dashRecoil)
            {
                // 回弹：横刀快速劈下（90° → -30°）→ 回正（0.3s）
                _dashRecoilT += Time.deltaTime / 0.3f;
                float rt = Mathf.Clamp01(_dashRecoilT);
                float z = rt < 0.5f
                    ? Mathf.Lerp(90f, -30f, rt * 2f)
                    : Mathf.Lerp(-30f, 0f, (rt - 0.5f) * 2f);
                blade.localRotation = Quaternion.Euler(0f, 0f, z);
                if (rt >= 1f) _dashRecoil = false;
            }
            else if (_swingAnim < 1f)
            {
                // 下劈：绕 X 轴从抬起 50° 劈到 -40°（easeOut 快速下劈）
                _swingAnim += Time.deltaTime / GameConfig.SwordArcSeconds;
                float t = Mathf.Clamp01(_swingAnim);
                float pitch = Mathf.Lerp(swingPitchUp, swingPitchDown, t * t); // 快下慢收
                blade.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            else if (_sword.IsBlocking)
            {
                // 格挡姿态：刀倾斜 30°（防御姿态）
                blade.localRotation = Quaternion.Slerp(blade.localRotation,
                    Quaternion.Euler(0f, 0f, 30f), 12f * Time.deltaTime);
            }
            else
            {
                // 默认持刀：竖持（回正）
                blade.localRotation = Quaternion.Slerp(blade.localRotation,
                    Quaternion.identity, 12f * Time.deltaTime);
            }
        }
    }

    // 挥砍范围伤害：前方 SwordRange 内所有 Health 目标（一刀 50；砍中回格挡条）
    private void SwingHit()
    {
        var hits = Physics.OverlapSphere(transform.position + transform.forward * (GameConfig.SwordRange * 0.5f),
            GameConfig.SwordRange);
        foreach (var c in hits)
        {
            // 玩家自身及其全部后代（CC/视觉/刀/任何子物体）——层级判断，不依赖 collider 列表
            if (c.transform.IsChildOf(transform)) continue;
            var targetComp = c.GetComponentInParent<HealthComponent>();
            if (targetComp == null) continue;
            DamageSystem.ApplyHit(targetComp.Logic, GameConfig.SwordDamage, null);
            OnSwordHit(); // 砍中回格挡条
        }
    }

    // 是否玩家自己的碰撞体（CC + 子物体）
    private bool IsMine(Collider c)
    {
        foreach (var mc in _myColliders)
            if (mc == c) return true;
        return false;
    }

    // 冲刺斩：沿相机朝向（完整方向，含俯仰——准星指哪冲哪）
    private void StartDash()
    {
        var cam = Camera.main;
        if (cam != null)
        {
            _dashDir = cam.transform.forward; // 完整视角方向（含俯仰）
            _dashDir.Normalize();
        }
        _dashing = true;
        _dashT = 0f;
        _swingAnim = 1f; // 冲刺不触发下劈动画（冲刺横刀 + 回弹自成一套）

        // 路径伤害：冲刺路径（0~6m）内所有目标一刀 50 + 回条
        var hits = Physics.OverlapSphere(transform.position + _dashDir * (GameConfig.DashAttackRange * 0.5f),
            GameConfig.DashAttackRange * 0.6f);
        foreach (var c in hits)
        {
            // 玩家自身及后代全跳过（层级判断）
            if (c.transform.IsChildOf(transform)) continue;
            var comp = c.GetComponentInParent<HealthComponent>();
            if (comp == null) continue;
            DamageSystem.ApplyHit(comp.Logic, GameConfig.SwordDamage, null);
            OnSwordHit();
        }
        // 穿过目标：冲刺期间忽略与 Health 目标的碰撞（结束恢复）
        foreach (var hc in FindObjectsByType<HealthComponent>(FindObjectsSortMode.None))
        {
            var col = hc.GetComponent<Collider>();
            if (col != null) Physics.IgnoreCollision(_cc, col, true);
        }
    }

    // 冲刺结束：恢复与目标的碰撞
    private void RestoreCollisions()
    {
        foreach (var hc in FindObjectsByType<HealthComponent>(FindObjectsSortMode.None))
        {
            var col = hc.GetComponent<Collider>();
            if (col != null) Physics.IgnoreCollision(_cc, col, false);
        }
    }

    // 供伤害系统查询：格挡状态与格挡条
    public bool IsBlocking => _sword.IsBlocking;
    public float Block => _sword.Block;
    public float DashCooldownRemain => _sword.DashCooldownRemain; // 冲刺冷却（HUD 显示）
    public SwordController GetBlocker() => _sword; // 目标持刀格挡时提供减伤
    public void OnSwordHit() => _sword.GainBlock(GameConfig.BlockGainOnHit); // 砍中回条

    // 后台冷却驱动（WeaponSwitch 常驻调用：切枪后 SwordView 禁用但冲刺冷却继续恢复）
    public void TickCooldowns(float dt) => _sword.Tick(dt);

    // 切枪显示控制（WeaponSwitch 调用：刀是相机子物体，需显隐管理）
    public void SetBladeVisible(bool visible)
    {
        if (blade != null) blade.gameObject.SetActive(visible);
    }
}

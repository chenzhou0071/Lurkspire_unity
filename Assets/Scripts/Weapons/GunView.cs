using UnityEngine;
using UnityEngine.InputSystem;

// GunView — 双枪表现层：鼠标左键连射（交替枪口闪光）/ 右键换弹 / 瞄准射线
// 挂载：玩家物体；两个枪口子物体（黑/白 Cube）拖到槽
public class GunView : MonoBehaviour
{
    [SerializeField] private Transform muzzleBlack; // 左枪口（黑枪）
    [SerializeField] private Transform muzzleWhite; // 右枪口（白枪）
    private GunController _gun = new GunController();
    private Camera _cam;
    private float _flashTimer;
    private int _lastShooter = -1;
    private Vector3 _blackBaseScale; // 枪口基础 Scale（Awake 记录，闪光在基础值上放大）
    private Vector3 _whiteBaseScale;
    private LineRenderer _line;      // 弹道线（调试用，从相机前方画——可能被遮挡）
    private float _lineTimer;
    private GameObject _spark;       // 枪口火花球（开火反馈，从枪口飞出）
    private Vector3 _sparkVel;
    private float _sparkTimer;
    private float _lockFlash;        // 锁头枪口大亮计时
    private bool _charging;          // 转枪蓄力中
    private float _chargeT;          // 蓄力进度
    private float _lastSpinAngle;    // 上一帧旋转角（增量旋转用）
    private Transform _spinningGun;  // 正在转的枪口（即将发射的那把）

    private void Awake()
    {
        _cam = Camera.main;
        if (muzzleBlack != null) _blackBaseScale = muzzleBlack.localScale;
        if (muzzleWhite != null) _whiteBaseScale = muzzleWhite.localScale;
        _line = gameObject.AddComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.startWidth = 0.06f; _line.endWidth = 0.06f;
        _line.positionCount = 2;
        _line.enabled = false;
        _spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _spark.name = "MuzzleSpark";
        _spark.transform.localScale = Vector3.one * 0.14f;
        var mat = _spark.GetComponent<Renderer>().material;
        mat.color = Color.yellow;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.yellow);
        Destroy(_spark.GetComponent<Collider>());
        _spark.SetActive(false);
    }

    private void Update()
    {
        _gun.Tick(Time.deltaTime);
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // 左键按住连射（射速由 FireInterval 冷却控制）
        if (mouse != null && mouse.leftButton.isPressed && _gun.TryFire(out int shooter))
        {
            FireRay(shooter);
            _flashTimer = 0.08f;
            _lastShooter = shooter;
        }
        // R 键手动换弹
        if (kb != null && kb.rKey.wasPressedThisFrame)
            _gun.Reload();
        // 弹匣打空自动换弹（需要 ReloadSeconds，期间不能开火——由逻辑层控制）
        if (_gun.Ammo <= 0)
            _gun.Reload();
        // 右键锁头：按下开始转枪蓄力（360° 由快变慢）→ 蓄力完成发射
        if (mouse != null && mouse.rightButton.wasPressedThisFrame && !_charging && _gun.Charge > 0)
        {
            _charging = true;
            _chargeT = 0f;
            _lastSpinAngle = 0f;
            _spinningGun = _gun.NextShooter == 0 ? muzzleBlack : muzzleWhite; // 转即将发射的枪
        }
        if (_charging)
        {
            _chargeT += Time.deltaTime / GameConfig.LockSpinSeconds;
            float t = Mathf.Clamp01(_chargeT);
            // easeOutQuad：角度 0→360，速度由快变慢（蓄力感）
            float angle = 360f * (1f - (1f - t) * (1f - t));
            if (_spinningGun != null)
                _spinningGun.Rotate(Vector3.up, angle - _lastSpinAngle);
            _lastSpinAngle = angle;
            if (t >= 1f)
            {
                _charging = false;
                if (_gun.TryLockOn(out int lockShooter))
                {
                    FireRay(lockShooter, 60f, GameConfig.LockOnDamage); // 锁头：50 伤害必中一击
                    _flashTimer = 0.15f; // 锁头闪光更明显
                    _lastShooter = lockShooter;
                    _lockFlash = 0.6f;   // 锁头提示（枪口持续亮）
                }
            }
        }

        // 枪口闪光：交替枪号对应左右枪口脉冲（在基础 Scale 上放大，不覆盖手动设置）
        bool flash = _flashTimer > 0f;
        float lockScale = _lockFlash > 0f ? 2.2f : 1f; // 锁头：枪口大亮
        if (muzzleBlack != null)
            muzzleBlack.localScale = (flash && _lastShooter == 0 ? 1.5f : 1f) * lockScale * _blackBaseScale;
        if (muzzleWhite != null)
            muzzleWhite.localScale = (flash && _lastShooter == 1 ? 1.5f : 1f) * lockScale * _whiteBaseScale;
        _flashTimer -= Time.deltaTime;
        _lockFlash -= Time.deltaTime;
        // 弹道线消失计时
        if (_lineTimer > 0f)
        {
            _lineTimer -= Time.deltaTime;
            if (_lineTimer <= 0f) _line.enabled = false;
        }
        // 火花球飞行 + 消失
        if (_sparkTimer > 0f)
        {
            _sparkTimer -= Time.deltaTime;
            _spark.transform.position += _sparkVel * Time.deltaTime;
            if (_sparkTimer <= 0f) _spark.SetActive(false);
        }
    }

    // 切枪显示控制（WeaponSwitch 调用：枪口是相机子物体，需显隐管理）
    public void SetMuzzlesVisible(bool visible)
    {
        if (muzzleBlack != null) muzzleBlack.gameObject.SetActive(visible);
        if (muzzleWhite != null) muzzleWhite.gameObject.SetActive(visible);
    }

    // 瞄准：第一人称子弹方向 = 相机 forward（准星方向，含俯仰）
    // 之前用"水平面求交"——平视时射线与水平面平行无交点，导致开火无输出（bug 根因）
    private void FireRay(int shooter)
    {
        FireRay(shooter, 30f, GameConfig.GunDamage);
    }

    // damage: 指定伤害（锁头传 LockOnDamage；默认调用走 GunDamage）
    private void FireRay(int shooter, float range, int damage = 0)
    {
        Transform muzzle = shooter == 0 ? muzzleBlack : muzzleWhite;
        Vector3 from = muzzle != null ? muzzle.position
            : _cam.transform.position + _cam.transform.forward * 0.5f;
        from += _cam.transform.forward * 0.3f; // 起点提前（防被胶囊遮挡）
        // 子弹方向：枪口 → 瞄准点（准星方向 range 处交点）——汇聚到准心，指哪打哪
        Vector3 aimPoint = _cam.transform.position + _cam.transform.forward * range;
        Vector3 dir = (aimPoint - from).normalized;
        // 命中检测：沿子弹飞行线找 Health 目标
        if (Physics.Raycast(from, dir, out var hit, range))
        {
            var targetComp = hit.collider.GetComponentInParent<HealthComponent>();
            if (targetComp != null)
                DamageSystem.ApplyHit(targetComp.Logic,
                    damage > 0 ? damage : GameConfig.GunDamage,
                    hit.collider.GetComponentInParent<SwordView>()?.GetBlocker());
        }
        var color = shooter == 0 ? Color.black : Color.white; // 黑枪黑线 / 白枪白线
        _line.startColor = color; _line.endColor = color;
        _line.SetPosition(0, from);
        _line.SetPosition(1, from + dir * range);
        _line.enabled = true;
        _lineTimer = 0.1f; // 弹道线显示 0.1 秒
        // 枪口火花球：从枪口沿瞄准方向飞出（开火反馈，必然可见）
        _spark.transform.position = from;
        _sparkVel = dir * 60f;
        _sparkTimer = 0.12f;
        _spark.SetActive(true);
    }
}

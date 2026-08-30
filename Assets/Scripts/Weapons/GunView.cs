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

    private void Awake()
    {
        _cam = Camera.main;
        if (muzzleBlack != null) _blackBaseScale = muzzleBlack.localScale;
        if (muzzleWhite != null) _whiteBaseScale = muzzleWhite.localScale;
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

        // 枪口闪光：交替枪号对应左右枪口脉冲（在基础 Scale 上放大，不覆盖手动设置）
        bool flash = _flashTimer > 0f;
        if (muzzleBlack != null)
            muzzleBlack.localScale = flash && _lastShooter == 0 ? _blackBaseScale * 1.5f : _blackBaseScale;
        if (muzzleWhite != null)
            muzzleWhite.localScale = flash && _lastShooter == 1 ? _whiteBaseScale * 1.5f : _whiteBaseScale;
        _flashTimer -= Time.deltaTime;
    }

    // 瞄准：鼠标屏幕点 → 世界水平面（玩家高度）→ 射线方向（Scene 视图可见弹道）
    private void FireRay(int shooter)
    {
        if (mousePos == Vector2.zero) return;
        var ray = _cam.ScreenPointToRay(mousePos);
        var plane = new Plane(Vector3.up, transform.position);
        if (plane.Raycast(ray, out float dist))
        {
            Vector3 target = ray.GetPoint(dist);
            Vector3 dir = (target - transform.position).normalized;
            Debug.DrawRay(transform.position + Vector3.up * 0.8f,
                dir * 30f, shooter == 0 ? Color.black : Color.white, 0.1f);
        }
    }

    private Vector2 mousePos => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
}

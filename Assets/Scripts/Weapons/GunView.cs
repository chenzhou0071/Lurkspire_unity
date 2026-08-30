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

        // 枪口闪光：交替枪号对应左右枪口脉冲（在基础 Scale 上放大，不覆盖手动设置）
        bool flash = _flashTimer > 0f;
        if (muzzleBlack != null)
            muzzleBlack.localScale = flash && _lastShooter == 0 ? _blackBaseScale * 1.5f : _blackBaseScale;
        if (muzzleWhite != null)
            muzzleWhite.localScale = flash && _lastShooter == 1 ? _whiteBaseScale * 1.5f : _whiteBaseScale;
        _flashTimer -= Time.deltaTime;
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

    // 瞄准：第一人称子弹方向 = 相机 forward（准星方向，含俯仰）
    // 之前用"水平面求交"——平视时射线与水平面平行无交点，导致开火无输出（bug 根因）
    private void FireRay(int shooter)
    {
        Vector3 dir = _cam.transform.forward;
        Transform muzzle = shooter == 0 ? muzzleBlack : muzzleWhite;
        Vector3 from = muzzle != null ? muzzle.position
            : _cam.transform.position + _cam.transform.forward * 0.5f;
        from += dir * 0.3f; // 起点提前 0.3（防枪口在胶囊体内时火花被遮挡）
        var color = shooter == 0 ? Color.black : Color.white; // 黑枪黑线 / 白枪白线
        _line.startColor = color; _line.endColor = color;
        _line.SetPosition(0, from);
        _line.SetPosition(1, from + dir * 30f);
        _line.enabled = true;
        _lineTimer = 0.1f; // 弹道线显示 0.1 秒
        // 枪口火花球：从枪口沿瞄准方向飞出（开火反馈，必然可见）
        _spark.transform.position = from;
        _sparkVel = dir * 60f;
        _sparkTimer = 0.12f;
        _spark.SetActive(true);
    }
}

// RemotePlayer — 远端玩家（敌人）渲染：服务端状态广播 → 胶囊 + 武器 + 姿态
// 由 NetworkClient 创建/更新；30Hz 位置 → 每帧插值平滑
using UnityEngine;

public class RemotePlayer : MonoBehaviour
{
    public uint UID { get; private set; }
    public string PlayerName { get; private set; }
    public float HP { get; private set; } = 100;

    private Vector3 _targetPos;
    private float _targetYaw;
    private bool _first = true;
    private byte _weapon = 255; // 缓存（检测切换）
    private byte _anim;
    private float _animTimer;   // 挥砍/冲刺动作播放时长
    private Transform _gunBlack, _gunWhite, _swordModel; // 持有武器模型（黑白双枪/黑刀——与本地同款配色）

    // URP 材质工厂（CreatePrimitive 默认 Standard 在 URP 下显示品红——必须换 Lit）
    private static Material MakeLit(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader != null ? shader : Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }

    public static RemotePlayer Create(uint uid, string name)
    {
        // 胶囊人形（代码生成：胶囊 + 颜色标记）
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = $"Remote_{uid}";
        var rp = go.AddComponent<RemotePlayer>();
        rp.UID = uid;
        rp.PlayerName = name;
        var c = go.GetComponent<Collider>();
        Object.Destroy(c); // 敌人不需要本地碰撞
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = MakeLit(uid % 2 == 0
            ? new Color(0.9f, 0.4f, 0.2f)
            : new Color(0.2f, 0.6f, 0.9f));
        // 武器模型（与本地玩家同款：黑白双枪/黑色武士刀——身体中前手臂位）
        rp.BuildWeapons();
        // 锁定标记（联机锁头反馈——目标头顶闪黄）
        if (go.GetComponent<TargetIndicator>() == null)
            go.AddComponent<TargetIndicator>();
        return rp;
    }

    private void BuildWeapons()
    {
        // 武器大小与本地玩家一致（本地参数：黑 ±0.4,-0.25,0.3 / 0.15×0.15×0.5；刀 0.2,0,0.4 / 0.08×0.6×0.08）
        // 第三人称：位置从"相对相机"换到"身体两侧手部"（大小不变）
        _gunBlack = MakePart("GunBlack", new Vector3(0.4f, 0.4f, 0.5f),
            new Vector3(0.15f, 0.15f, 0.5f), new Color(0.12f, 0.12f, 0.15f));
        _gunWhite = MakePart("GunWhite", new Vector3(-0.4f, 0.4f, 0.5f),
            new Vector3(0.15f, 0.15f, 0.5f), new Color(0.92f, 0.92f, 0.95f));
        // 刀（本地 0.08×0.6×0.08——右侧前持）
        _swordModel = MakePart("Sword", new Vector3(0.2f, 0.35f, 0.6f),
            new Vector3(0.08f, 0.6f, 0.08f), new Color(0.1f, 0.1f, 0.12f));
        _swordModel.gameObject.SetActive(false); // 默认双枪
    }

    private Transform MakePart(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        Object.Destroy(part.GetComponent<Collider>());
        part.GetComponent<MeshRenderer>().sharedMaterial = MakeLit(color);
        part.transform.SetParent(transform, false);
        part.transform.localPosition = pos;
        part.transform.localScale = scale;
        return part.transform;
    }

    // 应用服务端状态（30Hz 到达时）
    public void ApplyState(NetProtocol.PlayerState s)
    {
        HP = s.HP;
        _targetPos = new Vector3(s.X, s.Y + 1f, s.Z); // 胶囊中心 = 位置 + 1
        // 服务端 yaw（度）与 Unity Y 旋转同向（90°=+X）——直接用
        _targetYaw = s.Yaw;
        if (_first)
        {
            transform.position = _targetPos;
            _first = false;
        }
        // 武器切换（0=枪 1=刀）
        if (s.Weapon != _weapon)
        {
            _weapon = s.Weapon;
            if (_gunBlack != null) _gunBlack.gameObject.SetActive(s.Weapon == 0);
            if (_gunWhite != null) _gunWhite.gameObject.SetActive(s.Weapon == 0);
            if (_swordModel != null) _swordModel.gameObject.SetActive(s.Weapon == 1);
        }
        // 动作（挥砍/冲刺短暂播放——其余持续姿态）
        if (s.Anim == NetProtocol.AnimSwing || s.Anim == NetProtocol.AnimDash)
        {
            _animTimer = 0.3f; // 动作播 0.3s（客户端 30Hz 上报覆盖）
            _anim = s.Anim;
        }
        else
        {
            _anim = s.Anim; // 持续姿态（跑墙/滑铲）
        }
        // 死亡（HP=0）：隐藏（重生后 Show）
        if (s.HP <= 0)
            gameObject.SetActive(false);
    }

    public void RespawnAt(NetProtocol.PlayerState s)
    {
        gameObject.SetActive(true);
        _first = true;
        ApplyState(s);
    }

    private void Update()
    {
        if (_first) return;
        // 插值平滑（30Hz 广播 → 60fps 渲染）
        transform.position = Vector3.Lerp(transform.position, _targetPos, 16f * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.Euler(0, _targetYaw, 0), 12f * Time.deltaTime);

        // 姿态（局部倾斜/压扁——跑墙斜身、滑铲压低、挥砍摆刀）
        if (_animTimer > 0f)
        {
            _animTimer -= Time.deltaTime;
            if (_anim == NetProtocol.AnimSwing)
            {
                // 挥砍：刀绕 X 快速下劈（表现）
                float t = 1f - Mathf.Clamp01(_animTimer / 0.3f);
                float x = Mathf.Lerp(50f, -40f, t);
                if (_swordModel != null) _swordModel.localRotation = Quaternion.Euler(x, 0, 0);
            }
            else if (_anim == NetProtocol.AnimDash)
            {
                // 冲刺：刀横置
                if (_swordModel != null)
                    _swordModel.localRotation = Quaternion.Slerp(_swordModel.localRotation,
                        Quaternion.Euler(0, 0, 90f), 16f * Time.deltaTime);
            }
        }
        else
        {
            // 持续姿态：跑墙倾斜（墙右→左倾 / 墙左→右倾——与本地镜像一致）、滑铲压扁
            float tilt = 0f;
            if (_anim == NetProtocol.AnimWallLeft) tilt = -12f;   // 墙在右 → 左倾
            else if (_anim == NetProtocol.AnimWallRight) tilt = 12f; // 墙在左 → 右倾
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.Euler(0, _targetYaw, 0) * Quaternion.Euler(0, 0, tilt), 12f * Time.deltaTime);
            float sy = _anim == NetProtocol.AnimSlide ? 0.75f : 1f;
            Vector3 sc = transform.localScale;
            sc.y = Mathf.Lerp(sc.y, sy, 12f * Time.deltaTime);
            transform.localScale = sc;
            // 刀复位
            if (_swordModel != null && _weapon == 1)
                _swordModel.localRotation = Quaternion.Slerp(_swordModel.localRotation,
                    Quaternion.identity, 10f * Time.deltaTime);
        }
    }
}

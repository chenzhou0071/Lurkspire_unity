// NetworkClient — 联机客户端（单例）：TCP 连接 / 上报输入 / 接收状态
// 架构：后台线程读帧 → 主线程事件队列（Update 消费——Unity API 安全）
// 本地玩家：照常本地移动（M1 PlayerInput）——只上报输入；收到自己状态做
// 校正/重生传送；敌人 = RemotePlayer 胶囊（服务端位置渲染）
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkClient : MonoBehaviour
{
    public static NetworkClient I { get; private set; }

    [Header("连接")]
    [SerializeField] private string serverIP = "127.0.0.1";
    [SerializeField] private int serverPort = 7777;
    [SerializeField] private string roomName = "arena";
    [SerializeField] private bool autoConnect = true; // Play 即连 + 自动入房（双开零操作）

    public uint MyUID { get; private set; }
    public bool Connected { get; private set; }
    public bool Dead { get; private set; }          // 自己死亡中（重生倒计时）
    public float DeadRemain { get; private set; }   // 重生剩余秒
    public string ConnState { get; private set; } = "未连接";

    private TcpClient _tcp;
    private NetworkStream _stream;
    private Thread _reader;
    private volatile bool _running;
    private readonly object _writeLock = new object();
    private readonly ConcurrentQueue<Action> _events = new ConcurrentQueue<Action>();

    private float _reportTimer;
    private bool _lockFiredPending; // 锁头发射待上报（一次性消费——只报一帧）
    private bool _joined;
    private float _deadUntil;

    // 敌人表
    private readonly Dictionary<uint, RemotePlayer> _remotes = new Dictionary<uint, RemotePlayer>();

    private PlayerInput _playerInput;
    private Transform _playerBody;
    private HealthComponent _health; // 本地血量（同步服务端权威值）

    private void Awake()
    {
        I = this;
        Application.runInBackground = true; // 失焦后台继续跑（双开联调必需）
    }

    private void Start()
    {
        if (!Application.isEditor)
            Screen.SetResolution(1280, 720, false); // 客户端窗口 1K 级（非全屏）
        if (autoConnect) Connect();
    }

    private void OnDestroy()
    {
        Disconnect();
        if (I == this) I = null;
    }

    // ---- 连接管理 ----

    public void Connect()
    {
        if (_running) return;
        ConnState = "连接中...";
        try
        {
            _tcp = new TcpClient();
            _tcp.Connect(serverIP, serverPort);
            _stream = _tcp.GetStream();
            _running = true;
            _reader = new Thread(ReadLoop) { IsBackground = true };
            _reader.Start();
            ConnState = "已连接";
            Connected = true;
            Debug.Log($"NetworkClient: 已连接 {serverIP}:{serverPort}");
            if (autoConnect) SendJoin(); // 自动入房（双开零操作）
        }
        catch (Exception e)
        {
            ConnState = "连接失败（3 秒后自动重试）";
            Debug.LogWarning($"NetworkClient 连接失败: {e.Message}");
            Disconnect();
            _retryTimer = 3f; // 自动重试（服务端晚起也能连上）
        }
    }

    private float _retryTimer; // 连接失败自动重试倒计时

    public void Disconnect()
    {
        _running = false;
        try { if (_stream != null) _stream.Close(); } catch { }
        try { if (_tcp != null) _tcp.Close(); } catch { }
        _reader = null;
        Connected = false;
        _joined = false;
    }

    // ---- 后台读线程：帧 → 主线程事件 ----

    private void ReadLoop()
    {
        var reader = new FrameReaderNet(_stream);
        while (_running)
        {
            try
            {
                var f = reader.Next();
                if (f == null) break;
                var msgID = f.Value.MsgID;
                var body = f.Value.Body;
                _events.Enqueue(() => HandleFrame(msgID, body));
            }
            catch
            {
                break; // 断开
            }
        }
        _events.Enqueue(() =>
        {
            ConnState = "已断开";
            Connected = false;
            Debug.Log("NetworkClient: 连接断开");
        });
    }

    // ---- 帧分发（主线程）----

    private void HandleFrame(ushort msgID, byte[] body)
    {
        switch (msgID)
        {
            case NetProtocol.MsgHeartbeat:
                break; // 心跳回显忽略
            case NetProtocol.MsgBattleJoinOK:
                NetProtocol.DecodeJoinOK(body, out var room, out var uid, out var players);
                MyUID = uid;
                _joined = true;
                ConnState = $"房间 {room} (uid={uid})";
                Debug.Log($"JoinOK: room={room} uid={uid} 玩家{players.Length} 个");
                // 已有玩家 → 全部建敌人
                foreach (var p in players)
                    if (p.UID != uid) SpawnRemote(p);
                break;
            case NetProtocol.MsgBattleState:
                ApplyState(NetProtocol.DecodeState(body));
                break;
            case NetProtocol.MsgBattleHit:
                var h = NetProtocol.DecodeHit(body);
                if (h.Target == MyUID && !Dead) FlashDamage(); // 被打中红闪
                break;
            case NetProtocol.MsgBattleDeath:
                var d = NetProtocol.DecodeHit(body);
                Debug.Log($"击杀: {d.Shooter} 杀 {d.Target}");
                if (d.Target == MyUID) StartDeath();
                break;
            case NetProtocol.MsgBattleSettle:
                ShowSettle(NetProtocol.DecodeSettle(body));
                break;
            case NetProtocol.MsgBattleErr:
                Debug.LogError($"服务端错误: {System.Text.Encoding.UTF8.GetString(body ?? new byte[0])}");
                break;
        }
    }

    // ---- 状态应用（30Hz）----

    private void ApplyState(NetProtocol.PlayerState[] states)
    {
        // 清理幽灵：广播里没有的玩家 = 已离开/掉线 → 删除本地渲染
        var seen = new HashSet<uint>();
        foreach (var s in states) seen.Add(s.UID);
        var ghosts = new List<uint>();
        foreach (var uid in _remotes.Keys)
            if (!seen.Contains(uid)) ghosts.Add(uid);
        foreach (var uid in ghosts)
        {
            if (_remotes.TryGetValue(uid, out var g) && g != null)
                Destroy(g.gameObject);
            _remotes.Remove(uid);
            _board.Remove(uid); // 击杀栏同步清理
        }
        foreach (var s in states)
        {
            // 击杀栏数据（每人击杀/死亡）
            _board[s.UID] = new BoardEntry { UID = s.UID, Score = s.Score, Deaths = s.Deaths };
            if (s.UID == MyUID)
            {
                ApplySelf(s);
                continue;
            }
            if (!_remotes.TryGetValue(s.UID, out var rp))
                rp = SpawnRemote(s);
            rp.ApplyState(s);
            // 敌人死亡后重生（HP>0 且隐藏中 → 复活显示）
            if (s.HP > 0 && !rp.gameObject.activeSelf)
                rp.RespawnAt(s);
        }
    }

    private bool _selfPlaced; // 开局已对齐出生点（服务端位置）

    private void ApplySelf(NetProtocol.PlayerState s)
    {
        // 开局第一次收到自己 → 搬到服务端出生点（场景摆位与服务端对齐）
        if (!_selfPlaced)
        {
            _selfPlaced = true;
            TeleportSelf(s);
            return;
        }
        // 死亡中：重生传送（服务端广播复活位置）
        if (Dead && s.HP > 0)
        {
            Dead = false;
            DeadRemain = 0;
            _deadUntil = 0;
            LockControl(false); // 恢复操控
            TeleportSelf(s);
            if (OnRespawn != null) OnRespawn();
            Debug.Log("已重生");
        }
        // 血量同步（服务端权威 → 本地 Health → HUD 血条显示真实值）
        SyncSelfHP(s.HP);
    }

    private void SyncSelfHP(byte hp)
    {
        if (_health == null)
        {
            var pi = FindFirstObjectByType<PlayerInput>();
            if (pi == null) return;
            _health = pi.GetComponent<HealthComponent>();
        }
        if (_health != null) _health.Logic.SetHP(hp);
    }

    private RemotePlayer SpawnRemote(NetProtocol.PlayerState s)
    {
        var rp = RemotePlayer.Create(s.UID, $"玩家{s.UID}");
        rp.ApplyState(s);
        _remotes[s.UID] = rp;
        Debug.Log($"[Net] 新玩家 uid{s.UID} @ ({s.X:F0},{s.Y:F0},{s.Z:F0})"); // 联调观察
        return rp;
    }

    private void TeleportSelf(NetProtocol.PlayerState s)
    {
        // 本地玩家搬到服务端权威位置（开局/重生）
        if (_playerBody == null)
        {
            var pi = FindFirstObjectByType<PlayerInput>();
            if (pi == null) return;
            _playerBody = pi.transform;
        }
        var cc = _playerBody.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        _playerBody.position = new Vector3(s.X, s.Y, s.Z);
        if (cc != null) cc.enabled = true;
    }

    // ---- 死亡表现（自己）----

    public event Action OnRespawn;
    private float _damageFlash;
    private string _settleText;
    private float _settleTimer;
    private float _deathFlash; // 死亡红屏强度

    private void StartDeath()
    {
        Dead = true;
        _deadUntil = Time.time + 2f; // 服务端重生 2 秒
        DeadRemain = 2f;
        _deathFlash = 1f;
        LockControl(true); // 死亡锁操控（不能动/打——重生恢复）
        Debug.Log("你死了！2 秒后重生");
    }

    // 死亡期间禁用玩家操控组件（重生按原始状态恢复——刀模式不开枪）
    private readonly List<KeyValuePair<Behaviour, bool>> _ctrl = new List<KeyValuePair<Behaviour, bool>>();

    private void LockControl(bool dead)
    {
        if (dead)
        {
            _ctrl.Clear();
            var player = FindFirstObjectByType<PlayerInput>();
            if (player != null)
            {
                foreach (var b in player.GetComponentsInChildren<Behaviour>(true))
                {
                    if (b is PlayerInput || b is WeaponSwitch || b is GunView
                        || b is SwordView || b is Crosshair)
                    {
                        _ctrl.Add(new KeyValuePair<Behaviour, bool>(b, b.enabled)); // 记原始状态
                        b.enabled = false;
                    }
                }
            }
            var cam = Camera.main;
            if (cam != null)
            {
                var pc = cam.GetComponent<PlayerCamera>();
                if (pc != null) { _ctrl.Add(new KeyValuePair<Behaviour, bool>(pc, pc.enabled)); pc.enabled = false; }
            }
        }
        else
        {
            // 按原始状态恢复（刀模式下 GunView 原本 off——不能强开）
            foreach (var kv in _ctrl) kv.Key.enabled = kv.Value;
            _ctrl.Clear();
        }
    }

    private void FlashDamage() => _damageFlash = 0.5f;

    private void ShowSettle(NetProtocol.SettleEntry[] entries)
    {
        _settleText = "对局结束\n";
        for (int i = 0; i < entries.Length && i < 5; i++)
        {
            var e = entries[i];
            string mark = e.UID == MyUID ? " (你)" : "";
            _settleText += $"{i + 1}. 玩家{e.UID}{mark}: {e.Score} 杀\n";
        }
        _settleTimer = 8f;
    }

    // ---- 上报（GunView 锁头发射通知——静态判空）----
    public static void NotifyLockFired()
    {
        if (I != null) I._lockFiredPending = true;
    }

    // ---- 每帧：上报 + 事件消费 + HUD ----

    private void Update()
    {
        // Tab 击杀栏开关（按住显示——CF 风格）
        var kb = Keyboard.current;
        ScoreboardOpen = kb != null && kb.tabKey.isPressed;
        // 连接失败自动重试（每 3 秒——服务端晚起也能连上）
        if (!Connected && autoConnect && _retryTimer > 0f)
        {
            _retryTimer -= Time.deltaTime;
            if (_retryTimer <= 0f) Connect();
        }
        if (!Connected || !_joined || Dead) { if (Dead) TickDead(); DrainEvents(); return; }

        // 上报节流 30Hz（与服务端 tick 对齐——避免 60fps 上报 2 倍速积分）
        _reportTimer += Time.deltaTime;
        if (_reportTimer >= 1f / 30f)
        {
            _reportTimer = 0f;
            SendInputReport();
        }
        DrainEvents();

        // 玩家引用懒获取
        if (_playerInput == null)
        {
            _playerInput = FindFirstObjectByType<PlayerInput>();
            if (_playerInput != null) _playerBody = _playerInput.transform;
        }
    }

    private void TickDead()
    {
        DeadRemain = _deadUntil - Time.time;
        DrainEvents();
    }

    private void DrainEvents()
    {
        while (_events.TryDequeue(out var ev)) ev();
    }

    // ---- 构造并发送输入上报 ----

    private void SendInputReport()
    {
        if (_playerInput == null)
        {
            _playerInput = FindFirstObjectByType<PlayerInput>();
            if (_playerInput == null) return;
            _playerBody = _playerInput.transform;
        }
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool swordMode = false;
        var ws = FindFirstObjectByType<WeaponSwitch>();
        if (ws != null) swordMode = ws.Current == 2;

        var report = new NetProtocol.InputReport();
        var move = _playerInput.NetMove;
        report.MoveX = (sbyte)Mathf.Clamp(move.x, -1, 1);
        report.MoveY = (sbyte)Mathf.Clamp(move.y, -1, 1);

        byte btns = 0;
        if (_playerInput.NetWallRunning) btns |= NetProtocol.BtnWallRun;
        if (_playerInput.NetSliding) btns |= NetProtocol.BtnSlide;

        if (mouse != null)
        {
            if (swordMode)
            {
                if (mouse.leftButton.isPressed) btns |= NetProtocol.BtnSword;
                if (mouse.rightButton.isPressed) btns |= NetProtocol.BtnBlock;
            }
            else
            {
                if (mouse.leftButton.isPressed) btns |= NetProtocol.BtnFire;
            }
        }
        if (kb != null && kb.leftShiftKey.isPressed) btns |= NetProtocol.BtnDashAtk;
        // 锁头发射（一次性标志——上报一帧后清零，防服务端重复触发）
        if (_lockFiredPending)
        {
            btns |= NetProtocol.BtnLock;
            _lockFiredPending = false;
        }
        report.Buttons = btns;

        // 武器（0=枪 1=刀——其他人显示用）
        report.Weapon = (byte)(swordMode ? 1 : 0);
        // 动作码（其他人显示姿态用——跑墙带墙侧）
        int wallSide = _playerInput.NetWallSide;
        if (wallSide == 1) report.Anim = NetProtocol.AnimWallLeft;
        else if (wallSide == 2) report.Anim = NetProtocol.AnimWallRight;
        else if (_playerInput.NetSliding) report.Anim = NetProtocol.AnimSlide;
        else if (swordMode && mouse != null && mouse.leftButton.isPressed) report.Anim = NetProtocol.AnimSwing; // 挥砍中
        else report.Anim = NetProtocol.AnimGround;

        // 位置（本地移动结果——服务端验证后采纳；机动全保留）
        var pos = _playerBody.position;
        report.X = pos.x;
        report.Y = pos.y;
        report.Z = pos.z;
        // 朝向/瞄准（度）：与服务端同系（yaw 90°=+X——Unity Y 旋转同向，不取反）
        report.Yaw = _playerBody.eulerAngles.y;
        var cam = Camera.main;
        if (cam != null)
        {
            report.AimX = _playerBody.eulerAngles.y; // 水平（同 Yaw）
            report.AimY = -cam.transform.eulerAngles.x; // 俯仰（服务端 + 向上；Unity 抬头 euler.x 负）
        }

        Send(NetProtocol.MsgBattleInput, NetProtocol.EncodeInput(report));
    }

    public void Send(ushort msgID, byte[] body)
    {
        if (!_running || _stream == null) return;
        var frame = NetProtocol.EncodeFrame(msgID, 0, body);
        try
        {
            lock (_writeLock)
            {
                _stream.Write(frame, 0, frame.Length);
            }
        }
        catch { Disconnect(); }
    }

    public void SendJoin()
    {
        Send(NetProtocol.MsgBattleJoin, System.Text.Encoding.UTF8.GetBytes(roomName));
    }

    // 击杀栏开关（Tab 按住）
    public bool ScoreboardOpen { get; private set; }

    // 击杀栏数据（每玩家——来自 30Hz State）
    private class BoardEntry
    {
        public uint UID;
        public ushort Score, Deaths;
    }

    private readonly Dictionary<uint, BoardEntry> _board = new Dictionary<uint, BoardEntry>();

    // 击杀栏（半透明面板 8 行——CF 风格）
    private void DrawScoreboard()
    {
        float w = 500, h = 46 + 8 * 30 + 12;
        var rect = new Rect(Screen.width / 2f - w / 2, Screen.height / 2f - h / 2, w, h);
        // 半透明底
        GUI.color = new Color(0.1f, 0.1f, 0.15f, 0.75f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
        // 标题
        GUI.Label(new Rect(rect.x + 16, rect.y + 10, 200, 26), "击杀排行", ScoreTitleStyle());
        GUI.Label(new Rect(rect.x + rect.width - 190, rect.y + 10, 80, 26), "击杀", ScoreTitleStyle());
        GUI.Label(new Rect(rect.x + rect.width - 100, rect.y + 10, 80, 26), "死亡", ScoreTitleStyle());
        // 8 行（在线玩家 + 空位）
        var players = new List<BoardEntry>(_board.Values);
        players.Sort((a, b) => b.Score.CompareTo(a.Score)); // 击杀降序
        for (int i = 0; i < 8; i++)
        {
            float y = rect.y + 46 + i * 30;
            var row = new Rect(rect.x + 10, y, rect.width - 20, 28);
            if (i % 2 == 0)
            {
                GUI.color = new Color(1, 1, 1, 0.06f); // 斑马纹
                GUI.DrawTexture(row, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (i < players.Count)
            {
                var p = players[i];
                string name = p.UID == MyUID ? $"玩家{p.UID} (你)" : $"玩家{p.UID}";
                GUI.Label(new Rect(row.x + 8, y + 2, 300, 26), name, ScoreStyle());
                GUI.Label(new Rect(row.x + row.width - 190, y + 2, 80, 26), p.Score.ToString(), ScoreStyle());
                GUI.Label(new Rect(row.x + row.width - 100, y + 2, 80, 26), p.Deaths.ToString(), ScoreStyle());
            }
        }
    }

    private GUIStyle _scoreTitleStyle;
    private GUIStyle ScoreTitleStyle()
    {
        if (_scoreTitleStyle == null)
            _scoreTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleLeft };
        return _scoreTitleStyle;
    }

    private GUIStyle _scoreStyle;
    private GUIStyle ScoreStyle()
    {
        if (_scoreStyle == null)
            _scoreStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
        return _scoreStyle;
    }

    // ---- 屏幕 HUD（代码生成 Canvas——连接按钮/状态/死亡/受击）----
    // 复用 HUD 风格：极简——状态文字 + 死亡/结算显示（挂在 NetworkClient 上的 onGUI 简化）
    private void OnGUI()
    {
        // 连接状态（左上角——自动连接失败时显示原因）
        GUI.Label(new Rect(10, 10, 400, 30), ConnState);

        // 玩家列表（调试/碰头用：谁在哪多远）——仅未按 Tab 时显示
        if (_joined && _playerBody != null && !ScoreboardOpen)
        {
            string list = "玩家:\n";
            foreach (var kv in _remotes)
            {
                var rp = kv.Value;
                if (rp == null) continue;
                var to = rp.transform.position - _playerBody.position;
                float dist = to.magnitude;
                // 方向（相对自己朝向的左右前后）
                var fwd = _playerBody.forward;
                fwd.y = 0; fwd.Normalize();
                var right = _playerBody.right;
                right.y = 0; right.Normalize();
                float f = Vector3.Dot(fwd, to.normalized);
                float r = Vector3.Dot(right, to.normalized);
                string dir = f > 0.3f ? "前" : f < -0.3f ? "后" : "";
                dir += r > 0.3f ? "右" : r < -0.3f ? "左" : "";
                if (dir == "") dir = "那";
                list += $"uid{kv.Key}: {dir} {dist:F0}m\n";
            }
            GUI.Box(new Rect(10, 50, 200, 30 + _remotes.Count * 20), list);
        }

        // Tab 击杀栏（半透明——CF 风格计分板）
        if (_joined && ScoreboardOpen)
            DrawScoreboard();

        // 死亡红屏（强度淡出后保持低红）+ 倒计时
        if (Dead)
        {
            if (_deathFlash > 0) _deathFlash -= Time.deltaTime * 0.5f;
            GUI.color = new Color(1, 0, 0, Mathf.Max(0.25f, _deathFlash * 0.4f));
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height / 2f - 20, 400, 40),
                $"你死了！重生倒计时 {Mathf.CeilToInt(DeadRemain)}s", BigStyle());
        }
        // 受击红闪
        if (_damageFlash > 0)
        {
            _damageFlash -= Time.deltaTime;
            GUI.color = new Color(1, 0, 0, _damageFlash * 0.3f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        // 结算
        if (_settleTimer > 0)
        {
            _settleTimer -= Time.deltaTime;
            GUI.Box(new Rect(Screen.width / 2f - 150, Screen.height / 2f - 100, 300, 200), _settleText, BigStyle());
        }
    }

    private GUIStyle _bigStyle;
    private GUIStyle BigStyle()
    {
        if (_bigStyle == null)
        {
            _bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        }
        return _bigStyle;
    }
}

// ---- 后台读帧（NetworkStream 阻塞读——与 Go FrameReader 同构）----
public class FrameReaderNet
{
    private readonly NetworkStream _s;
    private readonly byte[] _header = new byte[12];
    private readonly byte[] _bodyBuf = new byte[NetProtocol.MaxBodySize];

    public FrameReaderNet(NetworkStream s) { _s = s; }

    public (ushort MsgID, byte[] Body)? Next()
    {
        int got = 0;
        while (got < 12)
        {
            int n = _s.Read(_header, got, 12 - got);
            if (n <= 0) return null;
            got += n;
        }
        if (NetProtocol.GetU16(_header, 0) != NetProtocol.Magic) return null;
        ushort msgID = NetProtocol.GetU16(_header, 2);
        uint len = NetProtocol.GetU32(_header, 8);
        if (len > NetProtocol.MaxBodySize) return null;
        byte[] body = null;
        if (len > 0)
        {
            body = new byte[len];
            got = 0;
            while (got < len)
            {
                int n = _s.Read(body, got, (int)len - got);
                if (n <= 0) return null;
                got += n;
            }
        }
        return (msgID, body);
    }
}

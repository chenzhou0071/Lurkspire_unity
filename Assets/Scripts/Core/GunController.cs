// GunController — 双枪逻辑（纯逻辑可测）：黑白交替/弹匣/冷却/换弹（需时）/充能锁头
public class GunController
{
    private int _alternate;      // 交替序列：0=黑 1=白
    private float _cooldown;
    private float _reloadTimer;
    private float _chargeTimer;
    public int Ammo { get; private set; } = GameConfig.MagazineSize;
    public bool IsReloading => _reloadTimer > 0f;
    public int Charge { get; private set; } // 锁头充能存量（10s 一发，最多存 3）
    public int NextShooter => _alternate;   // 下一发锁头的枪号（蓄力转枪表现用）

    // 当前充能进度（0~1）：满 3 发为 1；否则为正在充的那发进度（HUD 格子显示）
    public float ChargeProgress =>
        Charge >= GameConfig.ChargeMax ? 1f : _chargeTimer / GameConfig.ChargeSeconds;

    // 开火：交替枪号（0 黑 / 1 白）；弹匣空/冷却中/换弹中失败
    public bool TryFire(out int shooter)
    {
        shooter = _alternate;
        if (Ammo <= 0 || _cooldown > 0f || IsReloading) return false;
        _alternate = 1 - _alternate; // 黑白交替
        Ammo--;
        _cooldown = GameConfig.FireInterval;
        return true;
    }

    // 启动换弹：需要 ReloadSeconds 时间（期间不能开火）；满弹匣/已在换弹时忽略
    public void Reload()
    {
        if (IsReloading || Ammo >= GameConfig.MagazineSize) return;
        _reloadTimer = GameConfig.ReloadSeconds;
    }

    // 每帧调用：冷却倒计时 + 换弹计时（结束补满弹匣）+ 充能累计（10s 一发，存 3 封顶）
    public void Tick(float dt)
    {
        if (_cooldown > 0f) _cooldown -= dt;
        if (IsReloading)
        {
            _reloadTimer -= dt;
            if (_reloadTimer <= 0f)
            {
                _reloadTimer = 0f;
                Ammo = GameConfig.MagazineSize;
            }
        }
        if (Charge < GameConfig.ChargeMax)
        {
            _chargeTimer += dt;
            if (_chargeTimer >= GameConfig.ChargeSeconds)
            {
                _chargeTimer = 0f;
                Charge++;
            }
        }
    }

    // 锁头：消耗一发充能，返回交替枪号（第 1 次白枪 / 第 2 次黑枪）；无充能失败
    public bool TryLockOn(out int shooter)
    {
        shooter = _alternate;
        if (Charge <= 0) return false;
        _alternate = 1 - _alternate;
        Charge--;
        return true;
    }
}

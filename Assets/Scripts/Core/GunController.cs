// GunController — 双枪逻辑（纯逻辑可测）：黑白交替/弹匣/冷却/换弹（需时）
public class GunController
{
    private int _alternate;      // 交替序列：0=黑 1=白
    private float _cooldown;
    private float _reloadTimer;
    public int Ammo { get; private set; } = GameConfig.MagazineSize;
    public bool IsReloading => _reloadTimer > 0f;

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

    // 每帧调用：冷却倒计时 + 换弹计时（结束补满弹匣）
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
    }
}

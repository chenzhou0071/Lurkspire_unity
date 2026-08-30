using UnityEngine;

// TargetDummy — 靶子假人：可被打/砍倒下，3 秒恢复，计数
// 挂载：靶子物体（胶囊体/Cube）；HealthComponent 自动挂载
// 计分：T9 后 HUD/对局系统读取 HitsTaken
public class TargetDummy : MonoBehaviour
{
    public int HitsTaken { get; private set; } // 被命中次数（T9 对局统计用）
    private HealthComponent _healthComp;
    private float _resetTimer;
    private Quaternion _upRotation;

    private void Awake()
    {
        _healthComp = GetComponent<HealthComponent>();
        if (_healthComp == null) _healthComp = gameObject.AddComponent<HealthComponent>();
        _healthComp.Logic.OnDeath += OnDummyDeath;
        _upRotation = transform.rotation;
    }

    private void Update()
    {
        // 死亡：倒下（绕 X 转 90°）→ 3 秒后恢复
        bool dead = _healthComp.Logic.IsDead;
        if (dead)
        {
            _resetTimer -= Time.deltaTime;
            if (_resetTimer <= 0f) _healthComp.Logic.Reset();
        }
        Quaternion target = dead ? Quaternion.Euler(90f, 0f, 0f) : _upRotation;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 150f * Time.deltaTime);
    }

    private void OnDummyDeath()
    {
        HitsTaken++;
        _resetTimer = 3f;
    }
}

using UnityEngine;

// GameManager — 单机对局管理：重生/计分（骨架，M1 后续任务填充）
public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }
    public int Kills { get; private set; }

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
    }

    public void RegisterKill() => Kills++;
}

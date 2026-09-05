// NetProtocol — Lurkspire 网络协议 C# 镜像（与 Go internal/protocol 逐字节一致）
// 布局/大端/golden 值以 Go 仓库为权威（ProtocolTests 对拍钉死）
using System;
using System.IO;

public static class NetProtocol
{
    // ---- 帧协议 ----
    public const ushort Magic = 0x5344; // "SD"
    public const int HeaderSize = 12;
    public const int MaxBodySize = 64 * 1024;

    // ---- 消息号（与 msgid.go 一致）----
    public const ushort MsgLogin = 1;
    public const ushort MsgLoginResp = 2;
    public const ushort MsgHeartbeat = 9;
    public const ushort MsgBattleJoin = 300;
    public const ushort MsgBattleJoinOK = 301;
    public const ushort MsgBattleInput = 310;
    public const ushort MsgBattleState = 320;
    public const ushort MsgBattleHit = 330;
    public const ushort MsgBattleDeath = 331;
    public const ushort MsgBattleSettle = 340;
    public const ushort MsgBattleErr = 349;

    // ---- 按钮位（与 battle.go 一致）----
    public const byte BtnFire = 1 << 0;
    public const byte BtnSword = 1 << 1;
    public const byte BtnBlock = 1 << 2;
    public const byte BtnSlide = 1 << 3;
    public const byte BtnJump = 1 << 4;
    public const byte BtnDashAtk = 1 << 5;
    public const byte BtnWallRun = 1 << 6;
    public const byte BtnLock = 1 << 7;

    // ---- 大端读写 ----
    public static void PutU16(byte[] b, int off, ushort v) { b[off] = (byte)(v >> 8); b[off + 1] = (byte)v; }
    public static ushort GetU16(byte[] b, int off) { return (ushort)((b[off] << 8) | b[off + 1]); }
    public static void PutU32(byte[] b, int off, uint v)
    {
        b[off] = (byte)(v >> 24); b[off + 1] = (byte)(v >> 16);
        b[off + 2] = (byte)(v >> 8); b[off + 3] = (byte)v;
    }
    public static uint GetU32(byte[] b, int off)
    {
        return ((uint)b[off] << 24) | ((uint)b[off + 1] << 16) | ((uint)b[off + 2] << 8) | b[off + 3];
    }
    public static void PutF32(byte[] b, int off, float v)
    {
        byte[] raw = BitConverter.GetBytes(v);
        if (BitConverter.IsLittleEndian) Array.Reverse(raw); // 转大端
        Buffer.BlockCopy(raw, 0, b, off, 4);
    }
    public static float GetF32(byte[] b, int off)
    {
        byte[] raw = new byte[4];
        Buffer.BlockCopy(b, off, raw, 0, 4);
        if (BitConverter.IsLittleEndian) Array.Reverse(raw);
        return BitConverter.ToSingle(raw, 0);
    }

    // ---- 帧编解码（与 frame.go 一致）----
    public static byte[] EncodeFrame(ushort msgID, uint seq, byte[] body)
    {
        int bodyLen = body != null ? body.Length : 0;
        var b = new byte[HeaderSize + bodyLen];
        PutU16(b, 0, Magic);
        PutU16(b, 2, msgID);
        PutU32(b, 4, seq);
        PutU32(b, 8, (uint)bodyLen);
        if (bodyLen > 0) Buffer.BlockCopy(body, 0, b, HeaderSize, bodyLen);
        return b;
    }

    // ---- 战斗 DTO ----
    public struct PlayerState
    {
        public uint UID;
        public float X, Y, Z, Yaw;
        public byte HP;
        public byte Weapon; // 0=枪 1=刀
        public byte Alt;
        public float Block;
        public byte Anim;
        public ushort Score;  // 击杀数
        public ushort Deaths; // 死亡数
    }

    public struct InputReport
    {
        public sbyte MoveX, MoveY;
        public float Yaw;
        public byte Buttons;
        public float AimX, AimY; // 度（世界角）
        public float X, Y, Z;    // 本地位置（服务端验证后采纳）
        public byte Weapon;      // 0=枪 1=刀
        public byte Anim;        // 动作码（0地面 1跑墙 2滑铲 3空中 4挥砍 5冲刺）
    }

    // 动作码
    public const byte AnimGround = 0;
    public const byte AnimWallLeft = 1;  // 跑墙（墙在右——左倾）
    public const byte AnimWallRight = 2; // 跑墙（墙在左——右倾）
    public const byte AnimSlide = 3;
    public const byte AnimAir = 4;
    public const byte AnimSwing = 5;
    public const byte AnimDash = 6;

    public struct HitEvent
    {
        public uint Shooter, Target;
        public byte Damage;
        public bool Headshot;
    }

    public struct SettleEntry
    {
        public uint UID;
        public ushort Score;
    }

    // ---- State 编解码：count u8 + N×36 ----
    public static byte[] EncodeState(PlayerState[] states)
    {
        var b = new byte[1 + states.Length * 36];
        b[0] = (byte)states.Length;
        for (int i = 0; i < states.Length; i++)
        {
            var s = states[i];
            int o = 1 + i * 36;
            PutU32(b, o, s.UID);
            PutF32(b, o + 4, s.X); PutF32(b, o + 8, s.Y); PutF32(b, o + 12, s.Z);
            PutF32(b, o + 16, s.Yaw);
            b[o + 20] = s.HP; b[o + 21] = s.Weapon; b[o + 22] = s.Alt;
            PutF32(b, o + 23, s.Block);
            b[o + 27] = s.Anim;
            PutU16(b, o + 28, s.Score);
            PutU16(b, o + 30, s.Deaths);
            // o+32..35 预留
        }
        return b;
    }

    public static PlayerState[] DecodeState(byte[] b)
    {
        if (b == null || b.Length < 1) return new PlayerState[0];
        int n = b[0];
        if (b.Length < 1 + n * 36) return new PlayerState[0];
        var states = new PlayerState[n];
        for (int i = 0; i < n; i++)
        {
            int o = 1 + i * 36;
            states[i] = new PlayerState
            {
                UID = GetU32(b, o),
                X = GetF32(b, o + 4), Y = GetF32(b, o + 8), Z = GetF32(b, o + 12),
                Yaw = GetF32(b, o + 16),
                HP = b[o + 20], Weapon = b[o + 21], Alt = b[o + 22],
                Block = GetF32(b, o + 23),
                Anim = b[o + 27],
                Score = GetU16(b, o + 28),
                Deaths = GetU16(b, o + 30),
            };
        }
        return states;
    }

    // ---- Input 编解码：29B ----
    public static byte[] EncodeInput(InputReport r)
    {
        var b = new byte[29];
        b[0] = (byte)r.MoveX; b[1] = (byte)r.MoveY;
        PutF32(b, 2, r.Yaw);
        b[6] = r.Buttons;
        PutF32(b, 7, r.AimX); PutF32(b, 11, r.AimY);
        PutF32(b, 15, r.X); PutF32(b, 19, r.Y); PutF32(b, 23, r.Z);
        b[27] = r.Weapon; b[28] = r.Anim;
        return b;
    }

    public static InputReport DecodeInput(byte[] b)
    {
        if (b == null || b.Length < 29) return default;
        return new InputReport
        {
            MoveX = (sbyte)b[0], MoveY = (sbyte)b[1],
            Yaw = GetF32(b, 2), Buttons = b[6],
            AimX = GetF32(b, 7), AimY = GetF32(b, 11),
            X = GetF32(b, 15), Y = GetF32(b, 19), Z = GetF32(b, 23),
            Weapon = b[27], Anim = b[28],
        };
    }

    // ---- Hit 编解码：10B ----
    public static byte[] EncodeHit(HitEvent h)
    {
        var b = new byte[10];
        PutU32(b, 0, h.Shooter); PutU32(b, 4, h.Target);
        b[8] = h.Damage; b[9] = (byte)(h.Headshot ? 1 : 0);
        return b;
    }

    public static HitEvent DecodeHit(byte[] b)
    {
        if (b == null || b.Length < 10) return default;
        return new HitEvent
        {
            Shooter = GetU32(b, 0), Target = GetU32(b, 4),
            Damage = b[8], Headshot = b[9] != 0,
        };
    }

    // ---- Settle 编解码：count u8 + N×6 ----
    public static byte[] EncodeSettle(SettleEntry[] entries)
    {
        var b = new byte[1 + entries.Length * 6];
        b[0] = (byte)entries.Length;
        for (int i = 0; i < entries.Length; i++)
        {
            int o = 1 + i * 6;
            PutU32(b, o, entries[i].UID);
            PutU16(b, o + 4, entries[i].Score);
        }
        return b;
    }

    public static SettleEntry[] DecodeSettle(byte[] b)
    {
        if (b == null || b.Length < 1) return new SettleEntry[0];
        int n = b[0];
        if (b.Length < 1 + n * 6) return new SettleEntry[0];
        var entries = new SettleEntry[n];
        for (int i = 0; i < n; i++)
        {
            int o = 1 + i * 6;
            entries[i] = new SettleEntry { UID = GetU32(b, o), Score = GetU16(b, o + 4) };
        }
        return entries;
    }

    // ---- JoinOK 解码：房间名(u8 len) + selfUID(u32) + state ----
    public static void DecodeJoinOK(byte[] b, out string roomName, out uint selfUID, out PlayerState[] players)
    {
        roomName = ""; selfUID = 0; players = new PlayerState[0];
        if (b == null || b.Length < 5) return;
        int nameLen = b[0];
        if (b.Length < 1 + nameLen + 4) return;
        roomName = System.Text.Encoding.UTF8.GetString(b, 1, nameLen);
        selfUID = GetU32(b, 1 + nameLen);
        var rest = new byte[b.Length - (5 + nameLen)];
        Buffer.BlockCopy(b, 5 + nameLen, rest, 0, rest.Length);
        players = DecodeState(rest);
    }
}

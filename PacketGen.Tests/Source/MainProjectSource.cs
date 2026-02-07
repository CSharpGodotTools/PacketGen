using System;
using System.Collections.Generic;
using System.Text;

namespace PacketGen.Tests;

internal static class MainProjectSource
{
    private const string NetcodeNamespace = "Framework.Netcode";

    public static string NetExcludeAttribute => $$"""
        namespace {{NetcodeNamespace}};

        public sealed class NetExcludeAttribute : System.Attribute {}
        """;

    public static string PacketRegistryAttribute => $$"""
        namespace {{NetcodeNamespace}};

        [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class PacketRegistryAttribute : System.Attribute
        {
            public System.Type OpcodeType { get; }
        
            public PacketRegistryAttribute()
            {
                OpcodeType = typeof(byte);
            }
        
            public PacketRegistryAttribute(System.Type opcodeType)
            {
                OpcodeType = opcodeType;
            }
        }
        """;

    public static string PacketStubs => $$"""
        using System;

        namespace {{NetcodeNamespace}};

        public abstract class GamePacket
        {
            public virtual void Write(PacketWriter writer) { }
            public virtual void Read(PacketReader reader) { }
        }
        
        public abstract class ClientPacket : GamePacket { }
        public abstract class ServerPacket : GamePacket { }
        
        public class PacketWriter 
        {
            public void Write<T>(T v) { }
        }
        
        public class PacketReader 
        {
            public byte    ReadByte()           => 0;
            public sbyte   ReadSByte()          => 0;
            public char    ReadChar()           => '\0';
            public string  ReadString()         => string.Empty;
            public bool    ReadBool()           => false;
            public short   ReadShort()          => 0;
            public ushort  ReadUShort()         => 0;
            public int     ReadInt()            => 0;
            public uint    ReadUInt()           => 0;
            public float   ReadFloat()          => 0f;
            public double  ReadDouble()         => 0.0;
            public long    ReadLong()           => 0L;
            public ulong   ReadULong()          => 0UL;
            public byte[]  ReadBytes(int count) => new byte[count];
            public byte[]  ReadBytes()          => ReadBytes(ReadInt());
            public Godot.Vector2 ReadVector2()        => new Godot.Vector2(0f,0f);
            public Godot.Vector3 ReadVector3()        => new Godot.Vector3(0f,0f,0f);

            public T Read<T>() => default!;

            public object Read(Type t) => null!;
        }
        """;
}

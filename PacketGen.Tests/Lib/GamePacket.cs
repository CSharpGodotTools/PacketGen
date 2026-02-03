using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Netcode;

public abstract class GamePacket
{
    public static int MaxSize => 8192;

    public virtual void Write(PacketWriter writer)
    {
        // Handled by source generator
    }

    public virtual void Read(PacketReader reader)
    {
        // Handled by source generator
    }
}

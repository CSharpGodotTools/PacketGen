using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

public static class PacketRegistryGenerator
{
    public static string GetSource(List<INamedTypeSymbol> clientSymbols, List<INamedTypeSymbol> serverSymbols)
    {
        int clientOpcode = 0;
        int serverOpcode = 0;

        var clientEntries = new List<string>();
        var serverEntries = new List<string>();

        var namespaces = new HashSet<string>();

        // Process client packets
        foreach (var symbol in clientSymbols)
        {
            string typeName = symbol.Name;

            clientEntries.Add($@"
            {{
                typeof({typeName}),
                new PacketInfo<ClientPacket>
                {{
                    Opcode = {clientOpcode},
                    Instance = new {typeName}()
                }}
            }}"
            );

            clientOpcode++;

            string namespaceName = symbol.ContainingNamespace.ToDisplayString();
            namespaces.Add(namespaceName);
        }

        // Process server packets
        foreach (var symbol in serverSymbols)
        {
            string typeName = symbol.Name;

            serverEntries.Add($@"
            {{
                typeof({typeName}),
                new PacketInfo<ServerPacket>
                {{
                    Opcode = {serverOpcode},
                    Instance = new {typeName}()
                }}
            }}"
            );

            serverOpcode++;

            string namespaceName = symbol.ContainingNamespace.ToDisplayString();
            namespaces.Add(namespaceName);
        }

        var usings = string.Join("\n", namespaces.Select(ns => $"using {ns};"));

        // Generate the source code
        string sourceCode = $$"""
using System;
using System.Collections.Generic;
using System.Linq;
{{usings}}

namespace Framework.Netcode;

public static partial class PacketRegistry
{
    public static readonly Dictionary<Type, PacketInfo<ClientPacket>> ClientPacketInfo;
    public static readonly Dictionary<byte, Type> ClientPacketTypes;
    public static readonly Dictionary<Type, PacketInfo<ServerPacket>> ServerPacketInfo;
    public static readonly Dictionary<byte, Type> ServerPacketTypes;

    static PacketRegistry()
    {
        ClientPacketInfo = new Dictionary<Type, PacketInfo<ClientPacket>>()
        {
            {{string.Join(",\n", clientEntries)}}
        };

        ClientPacketTypes = ClientPacketInfo.ToDictionary(kvp => kvp.Value.Opcode, kvp => kvp.Key);

        ServerPacketInfo = new Dictionary<Type, PacketInfo<ServerPacket>>()
        {
            {{string.Join(",\n", serverEntries)}}
        };

        ServerPacketTypes = ServerPacketInfo.ToDictionary(kvp => kvp.Value.Opcode, kvp => kvp.Key);
    }
}

""";
        return sourceCode;
    }
}

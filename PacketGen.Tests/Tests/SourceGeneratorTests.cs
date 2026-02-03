using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VerifyXunit;
using Xunit;

namespace PacketGen.Tests;

[UsesVerify]
public class SourceGeneratorTests
{
    [Fact]
    public async Task GeneratesReadWriteForListProperty()
    {
        const string netcodeStubs = """
            using System.Collections.Generic;

            namespace Framework.Netcode;

            public abstract class GamePacket
            {
                public virtual void Write(PacketWriter writer) { }
                public virtual void Read(PacketReader reader) { }
            }

            public class ClientPacket : GamePacket { }
            public class ServerPacket : GamePacket { }

            public class PacketWriter
            {
                public void Write(int value) { }
            }

            public class PacketReader
            {
                public int ReadInt() => 0;
            }

            public class PacketInfo<T> where T : GamePacket
            {
                public byte Opcode { get; set; }
                public T Instance { get; set; }
            }

            public static partial class PacketRegistry { }
            """;

        const string packetSource = """
            using System.Collections.Generic;
            using Framework.Netcode;

            namespace Framework.Netcode.Tests;

            public partial class TestPacket : ServerPacket
            {
                public List<int> MyList { get; set; }
            }
            """;

        var compilation = CreateCompilation(new[] { netcodeStubs, packetSource });
        var generator = new PacketGen.Program();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var result = driver.GetRunResult();
        var generatedSources = result.Results
            .SelectMany(runResult => runResult.GeneratedSources)
            .OrderBy(source => source.HintName)
            .ToDictionary(source => source.HintName, source => source.SourceText.ToString());

        // Snapshot test: run `dotnet test` to create baseline files, then re-run to compare changes.
        await Verifier.Verify(generatedSources);
    }

    private static Compilation CreateCompilation(IEnumerable<string> sources)
    {
        var syntaxTrees = sources.Select(source => CSharpSyntaxTree.ParseText(source));
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "PacketGen.GeneratedTests",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

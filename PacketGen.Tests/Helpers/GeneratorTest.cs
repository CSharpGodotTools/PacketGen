using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Immutable;
using System.Reflection;

namespace PacketGen.Tests;

/// <summary>
/// Provides a test harness for running and validating source generators by generating code from test input and
/// retrieving the contents of a specified generated file.
/// </summary>
/// <typeparam name="TGenerator">The type of source generator to test. Must implement <see cref="IIncrementalGenerator"/> and have a public
/// parameterless constructor.</typeparam>
internal class GeneratorTest<TGenerator>(string testSource, string generatedFile) where TGenerator : IIncrementalGenerator, new()
{
    // Metadata references
    private readonly HashSet<string> _references =
    [
        // Default references
        typeof(object).Assembly.Location,
        typeof(Enumerable).Assembly.Location,
        typeof(Godot.Vector2).Assembly.Location,
        typeof(Godot.Vector3).Assembly.Location
    ];

    private readonly List<string> _sources = [testSource];

    static GeneratorTest()
    {
        // Delete all old generated files before tests start again
        string genDir = GeneratedFiles.GetGenDir();
        string[] genFiles = Directory.GetFiles(genDir);

        foreach (string genFile in genFiles)
        {
            File.Delete(genFile);
        }
    }

    /// <summary>
    /// Adds a metadata reference for the assembly that defines the specified type.
    /// </summary>
    /// <param name="type">The type whose containing assembly will be added as a metadata reference.</param>
    public void AddMetadataReference(Type type)
    {
        _references.Add(type.Assembly.Location);
    }

    public void AddSource(string source)
    {
        _sources.Add(source);
    }

    /// <summary>
    /// Generates source code using the configured source generator and returns the contents of the specified generated
    /// file.
    /// </summary>
    /// <remarks>This method parses the test code, runs the source generator, and verifies that the expected
    /// generated file exists. The generated file is also output to the designated directory for inspection. Use this
    /// method to obtain the generated source for further validation or analysis in source generator tests.</remarks>
    /// <returns>Returns null if the source file did not generate anything.</returns>
    public GeneratorTestResult? Start()
    {
        IEnumerable<SyntaxTree> syntaxTrees = _sources.Select(s => CSharpSyntaxTree.ParseText(s));

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: syntaxTrees,
            references: _references.Select(r => MetadataReference.CreateFromFile(r)),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        IIncrementalGenerator generator = new TGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        GeneratorDriverRunResult result = driver.GetRunResult();

        GeneratorRunResult runResult = result.Results.First();

        ImmutableArray<GeneratedSourceResult> generatedSources = runResult.GeneratedSources;

        // Source gen did not output anything
        if (generatedSources.Length == 0)
            return null;

        GeneratedSourceResult sourceResult = generatedSources.First();

        // Get the contents of the generated file
        string generatedSource = sourceResult.SourceText.ToString();

        // Output the generated file to bin\Debug\net10.0\_Generated
        GeneratedFiles.Output(generatedFile, generatedSource);

        return new GeneratorTestResult(generatedSource, generatedFile, _references, testSource);
    }
}

/// <summary>
/// Provides methods for accessing and previewing generated source code files for testing purposes.
/// </summary>
/// <param name="generatedFile">The path to the generated source file to be previewed.</param>
/// <param name="generatedSource">The source code content that has been generated.</param>
public class GeneratorTestResult(string generatedSource, string generatedFile, HashSet<string> _references, string testSource)
{
    /// <summary>
    /// Outputs the generated source to the <paramref name="source"/> parameter.
    /// </summary>
    public GeneratorTestResult GetGeneratedSource(out string source)
    {
        source = generatedSource;
        return this;
    }

    /// <summary>
    /// Previews the generated source file in the active IDE.
    /// </summary>
    public GeneratorTestResult PreviewSource()
    {
        GeneratedFiles.Preview(generatedFile);
        return this;
    }

    public record GeneratedCompilationResult(bool Success, IEnumerable<string> ReferencePaths, ImmutableArray<Diagnostic> Diagnostics, Assembly? Assembly, Exception? AssemblyException = null);

    public GeneratedCompilationResult CompileGeneratedAssembly(string generatedSource)
    {
        // Create syntax tree(s) for generated source
        SyntaxTree genTree = CSharpSyntaxTree.ParseText(generatedSource);

        // Combine test tree and generated tree into a new compilation
        var references = _references.Select(r => MetadataReference.CreateFromFile(r)).ToList();

        // Add common references usually needed by generated code
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));

        // Add reference to System.Runtime.dll
        string? trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        string? systemRuntimePath = trustedPlatformAssemblies?
            .Split(Path.PathSeparator)
            .FirstOrDefault(p => string.Equals(Path.GetFileName(p), "System.Runtime.dll", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(systemRuntimePath))
            references.Add(MetadataReference.CreateFromFile(systemRuntimePath));

        var referencePaths = references
            .OfType<PortableExecutableReference>()
            .Select(r => r.FilePath ?? string.Empty)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        var sourceTree = CSharpSyntaxTree.ParseText(testSource);

        SyntaxTree packetStubs = CSharpSyntaxTree.ParseText(MainProjectSource.PacketStubs);

        // Create compilation that includes the generated source
        CSharpCompilation genCompilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly" + "_Generated",
            syntaxTrees: [sourceTree, genTree, packetStubs],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        EmitResult emit = genCompilation.Emit(ms);

        var diagnostics = emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning).ToImmutableArray();

        if (!emit.Success)
        {
            return new GeneratedCompilationResult(false, referencePaths, diagnostics, null);
        }

        try
        {
            ms.Seek(0, SeekOrigin.Begin);
            Assembly loaded = Assembly.Load(ms.ToArray());
            return new GeneratedCompilationResult(true, referencePaths, diagnostics, loaded);
        }
        catch (Exception ex)
        {
            return new GeneratedCompilationResult(true, referencePaths, diagnostics, null, ex);
        }
    }
}

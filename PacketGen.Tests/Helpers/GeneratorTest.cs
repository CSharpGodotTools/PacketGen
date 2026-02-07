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
    private const string TestAssemblyName = "TestAssembly";

    // Metadata references
    private readonly HashSet<string> _references =
    [
        // Default references
        typeof(object).Assembly.Location,
        typeof(Enumerable).Assembly.Location,
        typeof(Godot.Vector2).Assembly.Location,
        typeof(Godot.Vector3).Assembly.Location
    ];

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
        SyntaxTree testTree = CSharpSyntaxTree.ParseText(testSource);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: TestAssemblyName,
            syntaxTrees: [testTree],
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

        return new GeneratorTestResult(generatedSource, generatedFile);
    }
}

/// <summary>
/// Provides methods for accessing and previewing generated source code files for testing purposes.
/// </summary>
/// <param name="generatedFile">The path to the generated source file to be previewed.</param>
/// <param name="generatedSource">The source code content that has been generated.</param>
public class GeneratorTestResult(string generatedSource, string generatedFile)
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
}

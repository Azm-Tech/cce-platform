using System.Collections.Immutable;
using System.Text;
using System.Threading;
using CCE.Domain.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CCE.Domain.SourceGenerators.Tests;

/// <summary>
/// Drives the <see cref="PermissionsGenerator"/> against an in-memory <c>permissions.yaml</c> string and
/// returns the generated <c>Permissions.g.cs</c> source text. Use <see cref="Run"/> in tests, then assert
/// against the returned string.
/// </summary>
internal static class GeneratorTestHarness
{
    public static string Run(string yaml)
    {
        var generator = new PermissionsGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTest",
            syntaxTrees: null,
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var additional = new InMemoryAdditionalText("permissions.yaml", yaml);
        driver = (CSharpGeneratorDriver)driver.AddAdditionalTexts(
            ImmutableArray.Create<AdditionalText>(additional));

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // The generator is expected to emit exactly one source file (Permissions.g.cs).
        // If it emits zero (empty YAML edge case), return empty string so tests can assert that explicitly.
        var generated = runResult.Results.SelectMany(r => r.GeneratedSources).FirstOrDefault();
        return generated.SourceText?.ToString() ?? string.Empty;
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly string _content;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _content = content;
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default)
            => SourceText.From(_content, Encoding.UTF8);
    }
}

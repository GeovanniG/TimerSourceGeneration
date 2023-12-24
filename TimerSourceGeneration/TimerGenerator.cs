using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TimerSourceGeneration;

[Generator]
public class TimerGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization required for this example
        Debug.WriteLine("TimerGenerator is executing...");
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // Retrieve the syntax trees of all user code
        var syntaxTrees = context.Compilation.SyntaxTrees;

        foreach (var syntaxTree in syntaxTrees)
        {
            // Traverse the syntax tree to find methods marked with TimerAttribute
            var root = syntaxTree.GetRoot();
            var methodNodes = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var methodNode in methodNodes)
            {
                // Check if the method has the TimerAttribute
                var attribute = methodNode.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .FirstOrDefault(attr => attr.Name.ToString() == nameof(TimerAttribute) || attr.Name.ToString() == "Timer");

                if (attribute != null)
                {
                    var classNode = methodNode.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();

                    var namespaceNode = methodNode.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();

                    // Generate code to measure execution time and print to console
                    var timerCode = GenerateTimerCode(context, syntaxTree, methodNode);

                    // Add the generated code to the compilation
                    context.AddSource($"{methodNode.Identifier}.g.cs", SourceText.From(timerCode, Encoding.UTF8));
                }
            }
        }
    }

    private string GenerateTimerCode(GeneratorExecutionContext context, SyntaxTree syntaxTree, MethodDeclarationSyntax methodNode)
    {
        var methodName = methodNode.Identifier.Text;
        var returnType = methodNode.ReturnType.ToString();
        var parameters = methodNode.ParameterList.Parameters;

        // Get the containing namespace and class
        var containingNamespace = GetContainingNamespace(context, syntaxTree, methodNode);
        var containingClass = GetContainingClass(methodNode);

        // Generate method parameters
        var parametersCode = string.Join(", ", parameters.Select(p => $"{p.Type} {p.Identifier}"));

        // Generate method invocation with parameters
        var methodInvocation = $"{methodName}({string.Join(", ", parameters.Select(p => p.Identifier))})";

        // Check if the return type is void
        var returnStatement = returnType == "void" ? "" : "return result;";

        // Generate code to measure execution time and print to console
        var timerCode = $@"
            namespace {containingNamespace}
            {{
                public static partial class {containingClass}
                {{
                    private static bool {methodName}HasBeenCalled {{ get; set; }} = false;

                    static partial {returnType} {methodName}({parametersCode})
                    {{
                        System.Diagnostics.Stopwatch stopwatch = null;
                        try
                        {{
                            if (!{methodName}HasBeenCalled)
                            {{
                                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                                {methodName}HasBeenCalled = true;
                                {methodInvocation};
                                {returnStatement}
                            }}
                        }}
                        finally
                        {{
                            stopwatch?.Stop();
                            if (stopwatch is not null)
                            {{
                                Console.WriteLine(""Execution time of {methodName}: "" + stopwatch.Elapsed);
                            }}
                        }}
                    }}
                }}
            }}";

        return timerCode;
    }

    private string GetContainingNamespace(GeneratorExecutionContext context, SyntaxTree syntaxTree, MethodDeclarationSyntax methodNode)
    {
        // Use the semantic model to get the symbol of the containing namespace
        var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);
        var namespaceSymbol = semanticModel.GetEnclosingSymbol(methodNode.SpanStart);

        // If the symbol is not found or it's a global namespace, return an empty string
        return namespaceSymbol?.ContainingSymbol?.ToDisplayString() ?? "";
    }

    private string GetContainingClass(MethodDeclarationSyntax methodNode)
    {
        // Traverse the syntax tree to find the containing class
        var classNode = methodNode.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        // If the class node is found, return its identifier
        return classNode?.Identifier.Text ?? "";
    }
}

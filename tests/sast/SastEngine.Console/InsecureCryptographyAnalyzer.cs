using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InsecureCryptographyAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SAST001";

    private static readonly LocalizableString Title = "Uso de Algoritmo Criptográfico Inseguro";

    private static readonly LocalizableString MessageFormat =
        "O algoritmo criptográfico '{0}' é considerado inseguro e não deve ser usado.";

    private static readonly LocalizableString Description =
        "Certos algoritmos criptográficos mais antigos (como MD5, SHA1, DES, TripleDES, RC4) possuem vulnerabilidades conhecidas e devem ser substituídos por alternativas mais fortes (como a família SHA-256 ou AES).";

    private const string Category = "Security";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    // Lista de nomes de algoritmos criptográficos inseguros
    private static readonly ImmutableArray<string> InsecureAlgorithmNames = ImmutableArray.Create(
        "MD5",
        "SHA1",
        "DES",
        "TripleDES",
        "RC4"
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Registra a análise para invocações de método
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

        // Registra a análise para criação de objetos (ex: new MD5CryptoServiceProvider())
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocationExpr = (InvocationExpressionSyntax)context.Node;

        // Verifica se a invocação é algo como "MD5.Create()" ou "SHA1.Create()"
        if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var typeName = memberAccess.Expression.ToString();
            if (InsecureAlgorithmNames.Contains(typeName, StringComparer.OrdinalIgnoreCase))
            {
                var diagnostic = Diagnostic.Create(Rule, invocationExpr.GetLocation(), typeName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreationExpr = (ObjectCreationExpressionSyntax)context.Node;
        var typeName = objectCreationExpr.Type.ToString();

        // Remove sufixos comuns como "CryptoServiceProvider" ou "Managed" para simplificar a detecção
        var simplifiedTypeName = typeName.Replace("CryptoServiceProvider", "").Replace("Managed", "");

        if (InsecureAlgorithmNames.Any(alg => simplifiedTypeName.Equals(alg, StringComparison.OrdinalIgnoreCase)))
        {
            var diagnostic = Diagnostic.Create(Rule, objectCreationExpr.GetLocation(), typeName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
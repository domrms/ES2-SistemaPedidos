using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class HardcodedSecretAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "SAST002";

    private static readonly LocalizableString Title = "Potencial Segredo Hardcoded Detectado";

    private static readonly LocalizableString MessageFormat =
        "Um potencial segredo ('{0}') foi encontrado hardcoded. Considere usar um cofre de segredos ou configuração externa.";

    private static readonly LocalizableString Description =
        "Segredos como senhas, chaves de API e connection strings não devem ser armazenados diretamente no código-fonte. Eles devem ser carregados de fontes seguras em tempo de execução.";

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

    // Lista de palavras-chave para identificar variáveis ou campos suspeitos
    private static readonly ImmutableArray<string> SuspiciousKeywords = ImmutableArray.Create(
        "password",
        "secret",
        "key",
        "token",
        "connectionstring",
        "pwd",
        "passwd"
    );

    // Regex para detectar padrões comuns de segredos dentro de strings
    private static readonly Regex SecretPatternRegex = new(
        @"(?i)\b(password|pwd|user\s*id|uid|server|data\s*source|initial\s*catalog|database|secret|token|api[_-]?key)\b\s*=\s*[^;]+",
        RegexOptions.Compiled);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Analisa declarações de variáveis
        context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclarator);

        // Analisa atribuições
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);

        // Analisa literais de string para padrões genéricos
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
    }

    private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
    {
        var declarator = (VariableDeclaratorSyntax)context.Node;
        if (declarator.Initializer?.Value is not LiteralExpressionSyntax literal) return;
        if (literal.Token.Kind() != SyntaxKind.StringLiteralToken) return;

        // Verifica se o nome da variável contém alguma palavra-chave suspeita
        if (SuspiciousKeywords.Any(keyword => declarator.Identifier.ValueText.ToLower().Contains(keyword)))
        {
            var diagnostic = Diagnostic.Create(Rule, declarator.GetLocation(), declarator.Identifier.ValueText);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;
        if (assignment.Right is not LiteralExpressionSyntax literal) return;
        if (literal.Token.Kind() != SyntaxKind.StringLiteralToken) return;

        // Verifica se o lado esquerdo da atribuição (a variável) tem um nome suspeito
        var variableName = (assignment.Left as IdentifierNameSyntax)?.Identifier.ValueText.ToLower();
        if (string.IsNullOrEmpty(variableName)) return;
        if (SuspiciousKeywords.Any(keyword => variableName.Contains(keyword)))
        {
            var diagnostic = Diagnostic.Create(Rule, assignment.GetLocation(), variableName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var literalExpression = (LiteralExpressionSyntax)context.Node;
        var literalText = literalExpression.Token.ValueText;

        // Verifica se o conteúdo da string bate com a regex de padrões de segredo
        if (SecretPatternRegex.IsMatch(literalText))
        {
            var diagnostic = Diagnostic.Create(Rule, literalExpression.GetLocation(),
                SecretPatternRegex.Match(literalText).Value);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
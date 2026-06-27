using System; // Added for Console and StringComparison
using System.IO; // Added for Path
using System.Linq; // Added for .Any() and .Where()
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis; // Added for Diagnostic, Location, DiagnosticSeverity
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Immutable;
using Task = System.Threading.Tasks.Task;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // 1. Inicializa o localizador do MSBuild
        if (!MSBuildLocator.IsRegistered) // Check if already registered
        {
            MSBuildLocator.RegisterDefaults();
        }

        int totalSecurityFlawsFound = 0; // Initialize counter

        // 2. Define os projetos alvo da análise, conforme especificado
        var targetProjectNames = new[]
        {
            "ES2-SistemaPedidos.Api",
            "ES2-SistemaPedidos.LambdaConsumerSQS",
            "ES2-SistemaPedidos.PersistenciaApi",
            "ES2-SistemaPedidos.Shared"
        };

        // 3. Resolve o caminho para a raiz da solução de forma robusta
        var solutionFilePath = FindSolutionFile(AppDomain.CurrentDomain.BaseDirectory);

        if (string.IsNullOrEmpty(solutionFilePath))
        {
            Console.WriteLine("Não foi possível encontrar o arquivo de solução (.sln) nos diretórios pais.");
            return 1; // Indicate failure
        }

        Console.WriteLine($"Solução identificada: {Path.GetFileName(solutionFilePath)}");

        // 4. Instancia as regras do SAST
        var analyzer = new InsecureCryptographyAnalyzer(); // Seu analisador Roslyn
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);

        // 5. Carrega e analisa a solução
        using (var workspace = MSBuildWorkspace.Create())
        {
            workspace.WorkspaceFailed += (s, e) => Console.WriteLine($"[MSBuild] {e.Diagnostic.Message}");

            var solution = await workspace.OpenSolutionAsync(solutionFilePath);

            // Filtra os projetos que realmente devem ser analisados, lidando com nomes de múltiplos alvos
            var projectsToAnalyze = solution.Projects
                .Where(p => targetProjectNames.Any(targetName => p.Name.StartsWith(targetName + "(", StringComparison.OrdinalIgnoreCase) || p.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (!projectsToAnalyze.Any())
            {
                Console.WriteLine("Nenhum dos projetos alvo foi encontrado na solução.");
                return 1; // Indicate failure if no target projects are found
            }

            foreach (var project in projectsToAnalyze)
            {
                Console.WriteLine($"[SAST] Analisando o projeto: {project.Name}"); // Corrected newline

                var compilation = await project.GetCompilationAsync();
                if (compilation == null)
                {
                    Console.WriteLine($"  Não foi possível obter a compilação para o projeto {project.Name}. Ignorando.");
                    continue;
                }

                var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
                var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

                var securityFlaws = diagnostics.Where(d => d.Id == InsecureCryptographyAnalyzer.DiagnosticId).ToList();

                if (!securityFlaws.Any())
                {
                    Console.WriteLine("  Nenhuma vulnerabilidade encontrada por esta regra.");
                }
                else
                {
                    foreach (var flaw in securityFlaws)
                    {
                        var lineSpan = flaw.Location.GetLineSpan();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  [ALERTA DE SEGURANÇA]");
                        Console.ResetColor();
                        Console.WriteLine($"  ID da Regra: {flaw.Id}");
                        Console.WriteLine($"  Arquivo: {Path.GetFileName(lineSpan.Path)}");
                        Console.WriteLine($"  Linha: {lineSpan.StartLinePosition.Line + 1}");
                        Console.WriteLine($"  Descrição: {flaw.GetMessage()}"); // Corrected newline
                        totalSecurityFlawsFound++; // Increment counter
                    }
                }
            }
        }

        Console.WriteLine("Varredura concluída."); // Corrected newline

        return totalSecurityFlawsFound > 0 ? 1 : 0; // Return 1 if flaws found, 0 otherwise
    }

    private static string FindSolutionFile(string currentPath)
    {
        var directory = new DirectoryInfo(currentPath);
        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }

        return directory?.GetFiles("*.sln").FirstOrDefault()?.FullName;
    }
}

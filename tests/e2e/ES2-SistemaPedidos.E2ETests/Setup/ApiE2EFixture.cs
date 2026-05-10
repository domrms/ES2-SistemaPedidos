using System.Text.Json;
using System.Text.Json.Serialization;
using Npgsql;
using Xunit;

namespace ES2_SistemaPedidos.E2ETests.Setup;

/// <summary>
/// Fixture para configurar os testes E2E com acesso à API e banco de dados
/// </summary>
public class ApiE2EFixture : IAsyncLifetime
{
    private readonly string _connectionString;
    public readonly HttpClient HttpClient;
    private NpgsqlConnection? _dbConnection;

    public ApiE2EFixture()
    {
        var apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:8080";
        _connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
            ?? "Host=localhost;Port=5432;Database=es2_pedidos;Username=dev;Password=dev";

        HttpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl), Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task InitializeAsync()
    {
        // Aguarda a API estar disponível
        await AguardarApiDisponivel();

        // Conecta ao banco de dados
        _dbConnection = new NpgsqlConnection(_connectionString);
        await _dbConnection.OpenAsync();

        // Prepara os dados de teste (insere cliente 9999 e produto 9999)
        await PrepararDadosTeste();
    }

    public async Task DisposeAsync()
    {
        if (_dbConnection != null)
        {
            await _dbConnection.CloseAsync();
            await _dbConnection.DisposeAsync();
        }

        HttpClient.Dispose();
    }

    private async Task AguardarApiDisponivel()
    {
        int tentativas = 0;
        const int maxTentativas = 30;

        while (tentativas < maxTentativas)
        {
            try
            {
                var response = await HttpClient.GetAsync("/api/healthcheck");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Continua tentando
            }

            tentativas++;
            await Task.Delay(1000);
        }

        throw new InvalidOperationException("API não respondeu após 30 segundos");
    }

    private async Task PrepararDadosTeste()
    {
        if (_dbConnection == null || _dbConnection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Conexão com banco de dados não está aberta");

        // Limpa eventos anteriores com cliente_id=9999 e produto_id=9999
        await LimparEventosTeste();

        // Verifica se cliente 9999 existe, se não cria
        var clienteExiste = await VerificarClienteExiste(9999);
        if (!clienteExiste)
        {
            await InserirCliente(9999, "Cliente E2E Test");
        }

        // Verifica se produto 9999 existe, se não cria
        var produtoExiste = await VerificarProdutoExiste(9999);
        if (!produtoExiste)
        {
            await InserirProduto(9999, "Produto E2E Test");
        }
    }

    public async Task LimparEventosTeste()
    {
        if (_dbConnection == null || _dbConnection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Conexão com banco de dados não está aberta");

        const string sql = "DELETE FROM eventos WHERE cliente_id = 9999 AND produto_id = 9999";
        using var command = new NpgsqlCommand(sql, _dbConnection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<bool> VerificarClienteExiste(int clienteId)
    {
        if (_dbConnection == null) throw new InvalidOperationException("Conexão não está aberta");

        const string sql = "SELECT COUNT(*) FROM clientes WHERE id = @id";
        using var command = new NpgsqlCommand(sql, _dbConnection);
        command.Parameters.AddWithValue("@id", clienteId);
        var count = (long?)await command.ExecuteScalarAsync() ?? 0;
        return count > 0;
    }

    private async Task<bool> VerificarProdutoExiste(int produtoId)
    {
        if (_dbConnection == null) throw new InvalidOperationException("Conexão não está aberta");

        const string sql = "SELECT COUNT(*) FROM produtos WHERE id = @id";
        using var command = new NpgsqlCommand(sql, _dbConnection);
        command.Parameters.AddWithValue("@id", produtoId);
        var count = (long?)await command.ExecuteScalarAsync() ?? 0;
        return count > 0;
    }

    private async Task InserirCliente(int id, string nome)
    {
        if (_dbConnection == null) throw new InvalidOperationException("Conexão não está aberta");

        const string sql = """
            INSERT INTO clientes (id, nome)
            VALUES (@id, @nome)
            ON CONFLICT (id) DO UPDATE SET nome = EXCLUDED.nome
            """;
        using var command = new NpgsqlCommand(sql, _dbConnection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@nome", nome);
        await command.ExecuteNonQueryAsync();
    }

    private async Task InserirProduto(int id, string nome)
    {
        if (_dbConnection == null) throw new InvalidOperationException("Conexão não está aberta");

        const string sql = """
            INSERT INTO produtos (id, nome)
            VALUES (@id, @nome)
            ON CONFLICT (id) DO UPDATE SET nome = EXCLUDED.nome
            """;
        using var command = new NpgsqlCommand(sql, _dbConnection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@nome", nome);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T?> ConsultarEventosAsync<T>()
    {
        using var response = await HttpClient.GetAsync("/api/solicitacoes/eventos");

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Error: {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<T>(content, options);
    }

    public async Task<T?> CriarSolicitacaoAsync<T>(int clienteId, int produtoId)
    {
        var payload = new { clienteId, produtoId };
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await HttpClient.PostAsync("/api/solicitacoes", content);

        var responseContent = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<T>(responseContent, options);
    }

    public async Task<List<EventoResponse>?> ObterEventosPorClienteEProdutoAsync(int clienteId, int produtoId)
    {
        if (_dbConnection == null || _dbConnection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Conexão com banco de dados não está aberta");

        const string sql = """
            SELECT e.id,
                   e.cliente_id,
                   e.produto_id,
                   e.evento_id,
                   e.data_hora_evento,
                   e.salvo_em,
                   c.nome AS nome_cliente,
                   p.nome AS nome_produto
              FROM eventos e
              JOIN clientes c ON c.id = e.cliente_id
              JOIN produtos p ON p.id = e.produto_id
             WHERE e.cliente_id = @cliente_id
               AND e.produto_id = @produto_id
             ORDER BY e.salvo_em
            """;

        using var command = new NpgsqlCommand(sql, _dbConnection);
        command.Parameters.AddWithValue("@cliente_id", clienteId);
        command.Parameters.AddWithValue("@produto_id", produtoId);

        var eventos = new List<EventoResponse>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            eventos.Add(new EventoResponse(
                reader.GetInt64(reader.GetOrdinal("id")),
                reader.GetString(reader.GetOrdinal("nome_cliente")),
                reader.GetString(reader.GetOrdinal("nome_produto")),
                reader.GetString(reader.GetOrdinal("evento_id")),
                reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("data_hora_evento")),
                reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("salvo_em")),
                reader.GetInt32(reader.GetOrdinal("cliente_id")),
                reader.GetInt32(reader.GetOrdinal("produto_id"))));
        }

        return eventos;
    }

    public async Task AguardarEventoSalvoNoBanco(int clienteId, int produtoId, string eventoId, int maxTentativas = 15)
    {
        int tentativas = 0;

        while (tentativas < maxTentativas)
        {
            var eventos = await ObterEventosPorClienteEProdutoAsync(clienteId, produtoId);
            if (eventos?.Any(e => e.EventoId == eventoId) == true)
            {
                return;
            }

            tentativas++;
            await Task.Delay(500);
        }

        throw new InvalidOperationException($"Evento {eventoId} não foi salvo no banco após {maxTentativas} tentativas");
    }
}

// DTOs para deserialização das respostas
public record RespostaEventosResponse(
    [property: JsonPropertyName("eventos")] List<EventoResponse>? Eventos);

public record EventoResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("nomeCliente")] string NomeCliente,
    [property: JsonPropertyName("nomeProduto")] string NomeProduto,
    [property: JsonPropertyName("eventoId")] string EventoId,
    [property: JsonPropertyName("dataHoraEvento")] DateTimeOffset DataHoraEvento,
    [property: JsonPropertyName("salvoEm")] DateTimeOffset SalvoEm,
    [property: JsonPropertyName("clienteId")] int? ClienteId = null,
    [property: JsonPropertyName("produtoId")] int? ProdutoId = null);

public record RespostaCriarSolicitacaoResponse(
    [property: JsonPropertyName("clienteId")] int ClienteId,
    [property: JsonPropertyName("produtoId")] int ProdutoId,
    [property: JsonPropertyName("eventoId")] string EventoId,
    [property: JsonPropertyName("dataHoraRequisicao")] DateTimeOffset DataHoraRequisicao);

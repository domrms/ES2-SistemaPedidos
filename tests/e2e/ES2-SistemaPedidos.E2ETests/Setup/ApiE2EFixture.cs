using System.Text.Json;
using ES2_SistemaPedidos.E2ETests.Support;
using Npgsql;
using Xunit;

namespace ES2_SistemaPedidos.E2ETests.Setup;

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
        await AguardarApiDisponivel();

        if (_dbConnection == null || _dbConnection.State != System.Data.ConnectionState.Open)
        {
            _dbConnection = new NpgsqlConnection(_connectionString);
            await _dbConnection.OpenAsync();
        }

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

    public async Task LimparEventosTeste()
    {
        if (_dbConnection == null || _dbConnection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Conexão com banco de dados não está aberta");

        const string sql = "DELETE FROM eventos WHERE cliente_id = @cliente_id AND produto_id = @produto_id";
        using var command = new NpgsqlCommand(sql, _dbConnection);
        command.Parameters.AddWithValue("@cliente_id", TestData.ClienteId);
        command.Parameters.AddWithValue("@produto_id", TestData.ProdutoId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T?> ConsultarEventosAsync<T>()
    {
        using var response = await HttpClient.GetAsync(ApiRoutes.Eventos);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Error: {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonDefaults.CaseInsensitive);
    }

    public async Task<HttpResponseMessage> EnviarSolicitacaoAsync(int clienteId, int produtoId)
    {
        var payload = new { clienteId, produtoId };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        return await HttpClient.PostAsync(ApiRoutes.Solicitacoes, content);
    }

    public async Task<RespostaCriarSolicitacaoResponse?> CriarSolicitacaoAsync(int clienteId, int produtoId)
    {
        using var response = await EnviarSolicitacaoAsync(clienteId, produtoId);
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RespostaCriarSolicitacaoResponse>(
            responseContent,
            JsonDefaults.CaseInsensitive);
    }

    public async Task<List<EventoResponse>> ObterEventosPorClienteEProdutoAsync(int clienteId, int produtoId)
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
        var tentativas = 0;

        while (tentativas < maxTentativas)
        {
            var eventos = await ObterEventosPorClienteEProdutoAsync(clienteId, produtoId);
            if (eventos.Any(e => e.EventoId == eventoId))
            {
                return;
            }

            tentativas++;
            await Task.Delay(500);
        }

        throw new InvalidOperationException($"Evento {eventoId} não foi salvo no banco após {maxTentativas} tentativas");
    }

    private async Task AguardarApiDisponivel()
    {
        var tentativas = 0;
        const int maxTentativas = 30;

        while (tentativas < maxTentativas)
        {
            try
            {
                var response = await HttpClient.GetAsync(ApiRoutes.HealthCheck);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // A API pode ainda estar subindo.
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

        await LimparEventosTeste();

        await InserirCliente(TestData.ClienteId, TestData.NomeCliente);
        await InserirProduto(TestData.ProdutoId, TestData.NomeProduto);
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
}

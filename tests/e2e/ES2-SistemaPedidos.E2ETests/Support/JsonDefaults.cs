using System.Text.Json;

namespace ES2_SistemaPedidos.E2ETests.Support;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Shared.Data;
using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.Api.Services;

public sealed class ServicoPedido(
    ApplicationDbContext contextoBanco,
    IPublicadorEventoPedido publicadorEvento,
    TimeProvider provedorTempo,
    IConfiguration configuracao)
{
    private const decimal ToleranciaValor = 0.01m;
    private const decimal ValorMaximo = 999_999.99m;
    private const int MaximoItens = 1000;
    private const int QuantidadeMaxima = 10_000;

    public async Task<Resultado<RespostaCriarPedido>> CreatePedidoAsync(
        RequisicaoCriarPedido requisicao,
        string correlacaoId,
        CancellationToken tokenCancelamento)
    {
        var errosValidacao = ValidateCriacaoPedido(requisicao);
        if (errosValidacao.Count > 0)
        {
            return Resultado<RespostaCriarPedido>.ValidationFailed(ToRespostaValidacao(errosValidacao));
        }

        var agora = provedorTempo.GetUtcNow();
        var pedido = new Pedido(Guid.NewGuid(), requisicao.ClienteId!.Trim(), requisicao.ValorTotal, agora);

        foreach (var item in requisicao.Itens!)
        {
            pedido.AddItemPedido(
                Guid.NewGuid(),
                item.ProdutoId!.Trim(),
                item.Quantidade,
                decimal.Round(item.PrecoUnitario, 2, MidpointRounding.AwayFromZero),
                string.IsNullOrWhiteSpace(item.Descricao) ? null : item.Descricao.Trim());
        }

        contextoBanco.Pedidos.Add(pedido);
        await contextoBanco.SaveChangesAsync(tokenCancelamento);

        var eventoPedidoCriado = ToEventoPedidoCriado(pedido, correlacaoId, agora);
        await publicadorEvento.PublishPedidoCriadoAsync(eventoPedidoCriado, tokenCancelamento);

        return Resultado<RespostaCriarPedido>.Success(new RespostaCriarPedido(
            pedido.Id,
            pedido.ClienteId,
            pedido.Status,
            pedido.ValorTotal,
            pedido.Itens.Count,
            pedido.CriadoEm,
            pedido.AtualizadoEm));
    }

    public async Task<RespostaDetalhesPedido?> GetPedidoPorIdAsync(Guid pedidoId, CancellationToken tokenCancelamento)
    {
        var pedido = await contextoBanco.Pedidos
            .AsNoTracking()
            .Include(entidade => entidade.Itens)
            .FirstOrDefaultAsync(entidade => entidade.Id == pedidoId, tokenCancelamento);

        return pedido is null ? null : ToRespostaDetalhes(pedido);
    }

    public async Task<Resultado<RespostaListarPedidos>> ListPedidosAsync(
        string clienteId,
        StatusPedido? status,
        int pular,
        int quantidade,
        DateOnly? dataDe,
        DateOnly? dataAte,
        CancellationToken tokenCancelamento)
    {
        var errosValidacao = ValidateListagemPedidos(clienteId, pular, quantidade, dataDe, dataAte);
        if (errosValidacao.Count > 0)
        {
            return Resultado<RespostaListarPedidos>.ValidationFailed(ToRespostaValidacao(errosValidacao));
        }

        var consulta = contextoBanco.Pedidos
            .AsNoTracking()
            .Include(pedido => pedido.Itens)
            .Where(pedido => pedido.ClienteId == clienteId.Trim());

        if (status.HasValue)
        {
            consulta = consulta.Where(pedido => pedido.Status == status.Value);
        }

        if (dataDe.HasValue)
        {
            var inicio = new DateTimeOffset(dataDe.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            consulta = consulta.Where(pedido => pedido.CriadoEm >= inicio);
        }

        if (dataAte.HasValue)
        {
            var fim = new DateTimeOffset(dataAte.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
            consulta = consulta.Where(pedido => pedido.CriadoEm <= fim);
        }

        var total = await consulta.CountAsync(tokenCancelamento);
        var pedidos = await consulta
            .OrderByDescending(pedido => pedido.CriadoEm)
            .Skip(pular)
            .Take(quantidade)
            .Select(pedido => new RespostaResumoPedido(
                pedido.Id,
                pedido.ClienteId,
                pedido.Status,
                pedido.ValorTotal,
                pedido.Itens.Count,
                pedido.CriadoEm,
                pedido.AtualizadoEm,
                pedido.ConcluidoEm))
            .ToListAsync(tokenCancelamento);

        return Resultado<RespostaListarPedidos>.Success(new RespostaListarPedidos(
            pedidos,
            new RespostaPaginacao(pular, quantidade, total, pular + pedidos.Count < total, (int)Math.Ceiling(total / (double)quantidade))));
    }

    private List<ErroValidacao> ValidateCriacaoPedido(RequisicaoCriarPedido requisicao)
    {
        var erros = new List<ErroValidacao>();

        if (string.IsNullOrWhiteSpace(requisicao.ClienteId))
        {
            erros.Add(new ErroValidacao("clienteId", "O clienteId e obrigatorio."));
        }
        else if (requisicao.ClienteId.Length > 255)
        {
            erros.Add(new ErroValidacao("clienteId", "O clienteId deve ter no maximo 255 caracteres."));
        }

        if (requisicao.ValorTotal <= 0)
        {
            erros.Add(new ErroValidacao("valorTotal", "O valorTotal deve ser maior que 0."));
        }
        else if (requisicao.ValorTotal > ValorMaximo)
        {
            erros.Add(new ErroValidacao("valorTotal", "O valorTotal deve ser no maximo 999999.99."));
        }

        if (requisicao.Itens is null || requisicao.Itens.Count == 0)
        {
            erros.Add(new ErroValidacao("itens", "Ao menos 1 item e obrigatorio."));
            return erros;
        }

        if (requisicao.Itens.Count > MaximoItens)
        {
            erros.Add(new ErroValidacao("itens", $"Um pedido nao pode conter mais de {MaximoItens} itens."));
        }

        var totalCalculado = 0m;
        var indice = 0;
        foreach (var item in requisicao.Itens)
        {
            var prefixo = $"itens[{indice}]";

            if (string.IsNullOrWhiteSpace(item.ProdutoId))
            {
                erros.Add(new ErroValidacao($"{prefixo}.produtoId", "O produtoId e obrigatorio."));
            }
            else if (item.ProdutoId.Length > 255)
            {
                erros.Add(new ErroValidacao($"{prefixo}.produtoId", "O produtoId deve ter no maximo 255 caracteres."));
            }

            if (item.Quantidade <= 0 || item.Quantidade > QuantidadeMaxima)
            {
                erros.Add(new ErroValidacao($"{prefixo}.quantidade", $"A quantidade deve estar entre 1 e {QuantidadeMaxima}."));
            }

            if (item.PrecoUnitario < 0 || item.PrecoUnitario > ValorMaximo)
            {
                erros.Add(new ErroValidacao($"{prefixo}.precoUnitario", "O precoUnitario deve estar entre 0 e 999999.99."));
            }

            if (item.Descricao?.Length > 500)
            {
                erros.Add(new ErroValidacao($"{prefixo}.descricao", "A descricao deve ter no maximo 500 caracteres."));
            }

            totalCalculado += decimal.Round(item.Quantidade * item.PrecoUnitario, 2, MidpointRounding.AwayFromZero);
            indice++;
        }

        if (Math.Abs(requisicao.ValorTotal - totalCalculado) > ToleranciaValor)
        {
            erros.Add(new ErroValidacao("valorTotal", $"O valorTotal deve corresponder a soma dos itens. Total calculado: {totalCalculado:0.00}."));
        }

        return erros;
    }

    private static List<ErroValidacao> ValidateListagemPedidos(string clienteId, int pular, int quantidade, DateOnly? dataDe, DateOnly? dataAte)
    {
        var erros = new List<ErroValidacao>();

        if (string.IsNullOrWhiteSpace(clienteId))
        {
            erros.Add(new ErroValidacao("clienteId", "O clienteId e obrigatorio."));
        }

        if (pular < 0)
        {
            erros.Add(new ErroValidacao("pular", "pular deve ser maior ou igual a 0."));
        }

        if (quantidade is < 1 or > 100)
        {
            erros.Add(new ErroValidacao("quantidade", "quantidade deve estar entre 1 e 100."));
        }

        if (dataDe.HasValue && dataAte.HasValue && dataDe.Value > dataAte.Value)
        {
            erros.Add(new ErroValidacao("dataDe", "dataDe deve ser anterior ou igual a dataAte."));
        }

        return erros;
    }

    private EventoPedidoCriado ToEventoPedidoCriado(Pedido pedido, string correlacaoId, DateTimeOffset publicadoEm)
    {
        var ambiente = configuracao["ASPNETCORE_ENVIRONMENT"] ?? "development";

        return new EventoPedidoCriado(
            $"evt-{Guid.NewGuid()}",
            "PedidoCriado",
            "1.0.0",
            publicadoEm,
            pedido.Id,
            pedido.ClienteId,
            pedido.ValorTotal,
            "EUR",
            pedido.Itens.Select(item => new ItemEventoPedido(
                item.ProdutoId,
                item.Quantidade,
                item.PrecoUnitario,
                item.ValorLinha,
                item.Descricao)).ToList(),
            correlacaoId,
            "es2-api",
            new Dictionary<string, string>
            {
                ["ambiente"] = ambiente,
                ["versaoApi"] = "1.0.0"
            });
    }

    private static RespostaDetalhesPedido ToRespostaDetalhes(Pedido pedido)
    {
        return new RespostaDetalhesPedido(
            pedido.Id,
            pedido.ClienteId,
            pedido.Status,
            pedido.ValorTotal,
            pedido.Itens.Select(item => new RespostaItemPedido(
                item.Id,
                item.ProdutoId,
                item.Quantidade,
                item.PrecoUnitario,
                item.ValorLinha,
                item.Descricao)).ToList(),
            pedido.CriadoEm,
            pedido.AtualizadoEm,
            pedido.ProcessamentoIniciadoEm,
            pedido.ConcluidoEm,
            pedido.MotivoAprovacao,
            pedido.MotivoRejeicao,
            pedido.MensagemErro);
    }

    private static RespostaErroValidacao ToRespostaValidacao(IReadOnlyCollection<ErroValidacao> errosValidacao)
    {
        return new RespostaErroValidacao(
            "ValidacaoFalhou",
            "A validacao do pedido falhou",
            errosValidacao);
    }
}

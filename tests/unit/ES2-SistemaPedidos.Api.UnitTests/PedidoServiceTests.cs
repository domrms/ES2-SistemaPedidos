using System.Text.RegularExpressions;
using ES2_SistemaPedidos.Api.Application.Abstractions;
using ES2_SistemaPedidos.Api.Application.Pedidos;
using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Shared.Domain.Repositories;
using ES2_SistemaPedidos.Shared.Domain;

namespace ES2_SistemaPedidos.Api.UnitTests;

public sealed partial class PedidoServiceTests
{
    private static readonly DateTimeOffset AgoraUtc = new(2026, 5, 3, 15, 4, 5, TimeSpan.Zero);

    [Fact]
    public async Task CriarSolicitacaoAsync_quando_cliente_id_invalido_retorna_erro_de_validacao()
    {
        var fixture = new Fixture();
        var servico = fixture.CriarServico();

        var resultado =
            await servico.CriarSolicitacaoAsync(new RequisicaoCriarSolicitacao(0, 10), CancellationToken.None);

        var erro = ExtrairErro(resultado);
        Assert.Equal("ValidacaoFalhou", erro.Erro);
        Assert.Collection(erro.Detalhes, detalhe =>
        {
            Assert.Equal("clienteId", detalhe.Campo);
            Assert.Equal("O clienteId deve ser maior que zero.", detalhe.Erro);
        });
        Assert.Equal(0, fixture.Clientes.Consultas);
        Assert.Equal(0, fixture.Produtos.Consultas);
        Assert.Empty(fixture.Publicador.Eventos);
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_quando_produto_id_invalido_retorna_erro_de_validacao()
    {
        var fixture = new Fixture();
        var servico = fixture.CriarServico();

        var resultado =
            await servico.CriarSolicitacaoAsync(new RequisicaoCriarSolicitacao(10, -1), CancellationToken.None);

        var erro = ExtrairErro(resultado);
        Assert.Collection(erro.Detalhes, detalhe =>
        {
            Assert.Equal("produtoId", detalhe.Campo);
            Assert.Equal("O produtoId deve ser maior que zero.", detalhe.Erro);
        });
        Assert.Equal(0, fixture.Clientes.Consultas);
        Assert.Equal(0, fixture.Produtos.Consultas);
        Assert.Empty(fixture.Publicador.Eventos);
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_quando_cliente_nao_existe_retorna_erro_de_validacao()
    {
        var fixture = new Fixture { ClienteExiste = false };
        var servico = fixture.CriarServico();

        var resultado =
            await servico.CriarSolicitacaoAsync(new RequisicaoCriarSolicitacao(99, 10), CancellationToken.None);

        var erro = ExtrairErro(resultado);
        Assert.Collection(erro.Detalhes, detalhe =>
        {
            Assert.Equal("clienteId", detalhe.Campo);
            Assert.Equal("Cliente 99 nao encontrado.", detalhe.Erro);
        });
        Assert.Equal(1, fixture.Clientes.Consultas);
        Assert.Equal(0, fixture.Produtos.Consultas);
        Assert.Empty(fixture.Publicador.Eventos);
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_quando_produto_nao_existe_retorna_erro_de_validacao()
    {
        var fixture = new Fixture { ProdutoExiste = false };
        var servico = fixture.CriarServico();

        var resultado =
            await servico.CriarSolicitacaoAsync(new RequisicaoCriarSolicitacao(20, 88), CancellationToken.None);

        var erro = ExtrairErro(resultado);
        Assert.Collection(erro.Detalhes, detalhe =>
        {
            Assert.Equal("produtoId", detalhe.Campo);
            Assert.Equal("Produto 88 nao encontrado.", detalhe.Erro);
        });
        Assert.Equal(1, fixture.Clientes.Consultas);
        Assert.Equal(1, fixture.Produtos.Consultas);
        Assert.Empty(fixture.Publicador.Eventos);
    }

    [Fact]
    public async Task CriarSolicitacaoAsync_quando_valida_publica_evento_e_retorna_resposta()
    {
        var fixture = new Fixture();
        var servico = fixture.CriarServico();

        var resultado =
            await servico.CriarSolicitacaoAsync(new RequisicaoCriarSolicitacao(7, 11), CancellationToken.None);

        var resposta = ExtrairSucesso(resultado);
        Assert.Equal(7, resposta.ClienteId);
        Assert.Equal(11, resposta.ProdutoId);
        Assert.Matches(EventoIdRegex(), resposta.EventoId);
        Assert.Equal(new DateTimeOffset(2026, 5, 3, 12, 4, 5, TimeSpan.FromHours(-3)), resposta.DataHoraRequisicao);

        var evento = Assert.Single(fixture.Publicador.Eventos);
        Assert.Equal(resposta.ClienteId, evento.ClienteId);
        Assert.Equal(resposta.ProdutoId, evento.ProdutoId);
        Assert.Equal(resposta.EventoId, evento.EventoId);
        Assert.Equal(resposta.DataHoraRequisicao, evento.DataHoraRequisicao);
    }

    [Fact]
    public async Task ListarEventosAsync_quando_nao_ha_eventos_retorna_lista_vazia()
    {
        var fixture = new Fixture { Eventos = [] };
        var servico = fixture.CriarServico();

        var resposta = await servico.ListarEventosAsync(CancellationToken.None);

        Assert.Empty(resposta.Eventos);
    }

    [Fact]
    public async Task ListarEventosAsync_quando_ha_eventos_retorna_lista_com_nomes()
    {
        var eventoDetalhado1 = new EventoClienteDetalhado(
            1,
            "Cliente A",
            "Produto X",
            "ES2-12345678-100000",
            new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 3, 10, 0, 5, TimeSpan.Zero));

        var eventoDetalhado2 = new EventoClienteDetalhado(
            2,
            "Cliente B",
            "Produto Y",
            "ES2-87654321-110000",
            new DateTimeOffset(2026, 5, 3, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 3, 11, 0, 5, TimeSpan.Zero));

        var fixture = new Fixture { Eventos = [eventoDetalhado1, eventoDetalhado2] };
        var servico = fixture.CriarServico();

        var resposta = await servico.ListarEventosAsync(CancellationToken.None);

        Assert.Equal(2, resposta.Eventos.Count);

        var primeiro = resposta.Eventos.First();
        Assert.Equal(1, primeiro.Id);
        Assert.Equal("Cliente A", primeiro.NomeCliente);
        Assert.Equal("Produto X", primeiro.NomeProduto);
        Assert.Equal("ES2-12345678-100000", primeiro.EventoId);

        var segundo = resposta.Eventos.Last();
        Assert.Equal(2, segundo.Id);
        Assert.Equal("Cliente B", segundo.NomeCliente);
        Assert.Equal("Produto Y", segundo.NomeProduto);
        Assert.Equal("ES2-87654321-110000", segundo.EventoId);
    }

    [Fact]
    public async Task ObterHistoricoAsync_quando_id_invalido_retorna_requisicao_invalida()
    {
        var fixture = new Fixture();
        var resultado = await fixture.CriarServico().ObterHistoricoAsync(0, CancellationToken.None);

        Assert.Equal(TipoResultadoConsulta.RequisicaoInvalida, resultado.Tipo);
        Assert.Equal("PedidoIdInvalido", resultado.Erro?.Erro);
        Assert.Equal(0, fixture.StatusRepositorio.Consultas);
    }

    [Fact]
    public async Task ObterHistoricoAsync_quando_pedido_nao_existe_retorna_nao_encontrado()
    {
        var fixture = new Fixture { Historico = null };
        var resultado = await fixture.CriarServico().ObterHistoricoAsync(42, CancellationToken.None);

        Assert.Equal(TipoResultadoConsulta.NaoEncontrado, resultado.Tipo);
        Assert.Equal("PedidoNaoEncontrado", resultado.Erro?.Erro);
    }

    [Fact]
    public async Task ObterHistoricoAsync_quando_pedido_existe_retorna_linha_do_tempo_ordenada()
    {
        var fixture = new Fixture
        {
            Historico = new HistoricoPedidoDetalhado(42, "ES2-12345678-120405",
            [
                new TransicaoPedidoDetalhada(1, EstadoPedido.Recebido, AgoraUtc, null),
                new TransicaoPedidoDetalhada(2, EstadoPedido.Concluido, AgoraUtc.AddSeconds(1), "Finalizado")
            ])
        };

        var resultado = await fixture.CriarServico().ObterHistoricoAsync(42, CancellationToken.None);

        Assert.Equal(TipoResultadoConsulta.Sucesso, resultado.Tipo);
        Assert.Equal(42, resultado.Valor?.PedidoId);
        Assert.Collection(resultado.Valor!.Historico,
            recebido => Assert.Equal(EstadoPedido.Recebido, recebido.Status),
            concluido =>
            {
                Assert.Equal(EstadoPedido.Concluido, concluido.Status);
                Assert.Equal("Finalizado", concluido.Detalhe);
            });
        Assert.Equal(TimeSpan.FromHours(-3), resultado.Valor.Historico.First().RegistradoEm.Offset);
    }

    private static RespostaErroValidacao ExtrairErro(Resultado<RespostaCriarSolicitacao> resultado)
    {
        return resultado.Match<RespostaErroValidacao?>(_ => null, erro => erro)!;
    }

    private static RespostaCriarSolicitacao ExtrairSucesso(Resultado<RespostaCriarSolicitacao> resultado)
    {
        return resultado.Match<RespostaCriarSolicitacao?>(resposta => resposta, _ => null)!;
    }

    [GeneratedRegex(@"^ES2-\d{8}-120405$")]
    private static partial Regex EventoIdRegex();

    private sealed class Fixture
    {
        public bool ClienteExiste { get; init; } = true;

        public bool ProdutoExiste { get; init; } = true;

        public IReadOnlyCollection<EventoClienteDetalhado> Eventos { get; init; } = [];

        public HistoricoPedidoDetalhado? Historico { get; init; }

        public FakeClienteRepositorio Clientes { get; private set; } = null!;

        public FakeProdutoRepositorio Produtos { get; private set; } = null!;

        public FakeEventoRepositorio EventoRepositorio { get; private set; } = null!;

        public FakePedidoStatusRepositorio StatusRepositorio { get; private set; } = null!;

        public FakePublicadorEventoSolicitacao Publicador { get; } = new();

        public PedidoService CriarServico()
        {
            Clientes = new FakeClienteRepositorio(ClienteExiste);
            Produtos = new FakeProdutoRepositorio(ProdutoExiste);
            EventoRepositorio = new FakeEventoRepositorio(Eventos);
            StatusRepositorio = new FakePedidoStatusRepositorio(Historico);

            return new PedidoService(Clientes, Produtos, EventoRepositorio, Publicador, new FakeTimeProvider(AgoraUtc),
                StatusRepositorio);
        }
    }

    private sealed class FakePedidoStatusRepositorio(HistoricoPedidoDetalhado? historico) : IPedidoStatusRepositorio
    {
        public int Consultas { get; private set; }

        public Task<HistoricoPedidoDetalhado?> ObterHistoricoAsync(long pedidoId,
            CancellationToken tokenCancelamento)
        {
            Consultas++;
            return Task.FromResult(historico);
        }
    }

    private sealed class FakeClienteRepositorio(bool existe) : IClienteRepositorio
    {
        public int Consultas { get; private set; }

        public Task<bool> ExisteClienteAsync(int clienteId, CancellationToken tokenCancelamento)
        {
            Consultas++;
            return Task.FromResult(existe);
        }
    }

    private sealed class FakeProdutoRepositorio(bool existe) : IProdutoRepositorio
    {
        public int Consultas { get; private set; }

        public Task<bool> ExisteProdutoAsync(int produtoId, CancellationToken tokenCancelamento)
        {
            Consultas++;
            return Task.FromResult(existe);
        }
    }

    private sealed class FakeEventoRepositorio(IReadOnlyCollection<EventoClienteDetalhado> eventos) : IEventoRepositorio
    {
        public Task<IReadOnlyCollection<EventoClienteDetalhado>> ListarTodosEventosAsync(
            CancellationToken tokenCancelamento)
        {
            return Task.FromResult(eventos);
        }
    }

    private sealed class FakePublicadorEventoSolicitacao : IPublicadorEventoSolicitacao
    {
        public List<EventoSolicitacaoCliente> Eventos { get; } = [];

        public Task PublicarAsync(EventoSolicitacaoCliente evento, CancellationToken tokenCancelamento)
        {
            Eventos.Add(evento);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset agoraUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return agoraUtc;
        }
    }
}

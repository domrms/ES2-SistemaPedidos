using System.Text.RegularExpressions;
using ES2_SistemaPedidos.Api;
using ES2_SistemaPedidos.Api.Application.Abstractions;
using ES2_SistemaPedidos.Api.Application.Pedidos;
using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Shared.Domain.Repositories;

namespace ES2_SistemaPedidos.Api.UnitTests;

public sealed partial class PedidoServiceTests
{
    private static readonly DateTimeOffset AgoraUtc = new(2026, 5, 3, 15, 4, 5, TimeSpan.Zero);

    [Fact]
    public async Task CriarSolicitacaoAsync_quando_cliente_id_invalido_retorna_erro_de_validacao()
    {
        var fixture = new Fixture();
        var servico = fixture.CriarServico();

        var resultado = await servico.CriarSolicitacaoAsync(new RequisicaoCriarSolicitacao(0, 10), CancellationToken.None);

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

        var resultado = await servico.CriarSolicitacaoAsync(new RequisicaoCriarSolicitacao(10, -1), CancellationToken.None);

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

        var resultado = await servico.CriarSolicitacaoAsync(new RequisicaoCriarSolicitacao(99, 10), CancellationToken.None);

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

        var resultado = await servico.CriarSolicitacaoAsync(new RequisicaoCriarSolicitacao(20, 88), CancellationToken.None);

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

        var resultado = await servico.CriarSolicitacaoAsync(new RequisicaoCriarSolicitacao(7, 11), CancellationToken.None);

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

        public FakeClienteRepositorio Clientes { get; private set; } = null!;

        public FakeProdutoRepositorio Produtos { get; private set; } = null!;

        public FakePublicadorEventoSolicitacao Publicador { get; } = new();

        public PedidoService CriarServico()
        {
            Clientes = new FakeClienteRepositorio(ClienteExiste);
            Produtos = new FakeProdutoRepositorio(ProdutoExiste);

            return new PedidoService(Clientes, Produtos, Publicador, new FakeTimeProvider(AgoraUtc));
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
        public override DateTimeOffset GetUtcNow() => agoraUtc;
    }
}

import { beforeEach, describe, expect, it, vi } from 'vitest';

const html = `
  <div id="healthResponse"></div>
  <div id="solicitacaoResponse"></div>
  <div id="eventosResponse"></div>
  <div id="eventosList"></div>
  <div id="historicoResponse"></div>
  <div id="historicoList"></div>
  <div id="healthStatus"><span class="status-indicator loading"></span></div>
  <span id="healthText">Verificando...</span>
  <form id="solicitacaoForm">
    <input id="clienteId" />
    <input id="produtoId" />
  </form>
  <form id="historicoForm"><input id="pedidoId" /></form>
`;

interface MockResponseOptions {
  ok?: boolean;
  status?: number;
  statusText?: string;
}

async function loadScript(): Promise<void> {
  vi.resetModules();
  document.body.innerHTML = html;
  await import('./script.ts');
  (document.getElementById('clienteId') as HTMLInputElement).value = '7';
  (document.getElementById('produtoId') as HTMLInputElement).value = '3';
  (document.getElementById('pedidoId') as HTMLInputElement).value = '42';
}

function response(body: unknown, options: MockResponseOptions = {}): object {
  const ok = options.ok ?? true;
  return {
    ok,
    status: options.status ?? (ok ? 200 : 400),
    statusText: options.statusText ?? (ok ? 'OK' : 'Bad Request'),
    json: vi.fn().mockResolvedValue(body)
  };
}

function mockFetch(body: unknown, options: MockResponseOptions = {}): ReturnType<typeof vi.fn> {
  const fetchMock = vi.fn().mockResolvedValue(response(body, options));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

const healthHealthy = {
  estado: 'healthy',
  verificacoes: {
    postgresql: { estado: 'healthy', descricao: 'Conexão disponível.' },
    floci: { estado: 'healthy', descricao: 'Floci acessível.' }
  }
};

describe('frontend script', () => {
  beforeEach(() => {
    vi.unstubAllGlobals();
    vi.useFakeTimers();
  });

  it('atualiza o indicador automático usando o estado agregado', async () => {
    mockFetch(healthHealthy);
    await loadScript();

    document.dispatchEvent(new Event('DOMContentLoaded'));
    await vi.waitFor(() => expect(document.getElementById('healthText')?.textContent)
      .toContain('Serviços operacionais'));

    expect(document.querySelector('.status-indicator')?.classList.contains('success')).toBe(true);
    await vi.advanceTimersByTimeAsync(30000);
    expect(fetch).toHaveBeenCalledTimes(2);
  });

  it('marca o indicador automático como desconectado quando a API não responde', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('sem conexão')));
    await loadScript();

    document.dispatchEvent(new Event('DOMContentLoaded'));
    await vi.waitFor(() => expect(document.getElementById('healthText')?.textContent).toContain('Desconectado'));

    expect(document.querySelector('.status-indicator')?.classList.contains('error')).toBe(true);
  });

  it('renderiza os diagnósticos avançados do healthcheck', async () => {
    const fetchMock = mockFetch(healthHealthy);
    await loadScript();
    await window.checkHealth();

    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:8080/api/healthcheck',
      { method: 'GET', headers: { Accept: '*/*' } }
    );
    expect(document.getElementById('healthResponse')?.classList.contains('success')).toBe(true);
    expect(document.querySelectorAll('.health-detail')).toHaveLength(2);
    expect(document.getElementById('healthResponse')?.textContent).toContain('PostgreSQL');
    expect(document.getElementById('healthResponse')?.textContent).toContain('Floci acessível');
  });

  it('identifica a dependência indisponível mesmo quando a API responde 503', async () => {
    mockFetch({
      estado: 'unhealthy',
      verificacoes: {
        postgresql: { estado: 'healthy', descricao: 'Disponível' },
        floci: { estado: 'unhealthy', descricao: 'Floci indisponível.' }
      }
    }, { ok: false, status: 503, statusText: 'Service Unavailable' });
    await loadScript();
    await window.checkHealth();

    expect(document.getElementById('healthResponse')?.classList.contains('error')).toBe(true);
    expect(document.getElementById('healthResponse')?.textContent).toContain('Indisponível');
    expect(document.querySelectorAll('.health-detail.unhealthy')).toHaveLength(1);
  });

  it('mostra falha de conexão ao consultar saúde', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('API indisponível')));
    await loadScript();
    await window.checkHealth();

    expect(document.getElementById('healthResponse')?.classList.contains('error')).toBe(true);
    expect(document.getElementById('healthResponse')?.textContent).toContain('API indisponível');
  });

  it('cria uma solicitação válida e limpa o formulário', async () => {
    const fetchMock = mockFetch({ eventoId: 'evt-1' });
    await loadScript();
    await window.criarSolicitacao(new SubmitEvent('submit'));

    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:8080/api/solicitacoes',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ clienteId: 7, produtoId: 3 })
      })
    );
    expect(document.getElementById('solicitacaoResponse')?.classList.contains('success')).toBe(true);
    expect((document.getElementById('clienteId') as HTMLInputElement).value).toBe('');
  });

  it('bloqueia IDs inválidos antes de enviar a solicitação', async () => {
    const fetchMock = mockFetch({});
    await loadScript();
    (document.getElementById('clienteId') as HTMLInputElement).value = '0';
    await window.criarSolicitacao(new SubmitEvent('submit'));

    expect(fetchMock).not.toHaveBeenCalled();
    expect(document.getElementById('solicitacaoResponse')?.textContent).toContain('maiores que zero');
  });

  it('exibe a mensagem de validação devolvida pela API', async () => {
    mockFetch({ erro: 'ValidacaoFalhou', mensagem: 'Cliente não encontrado.' }, { ok: false });
    await loadScript();
    await window.criarSolicitacao(new SubmitEvent('submit'));

    expect(document.getElementById('solicitacaoResponse')?.textContent).toContain('Cliente não encontrado');
  });

  it('renderiza pedidos e abre o histórico pelo atalho do card', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(response({
        eventos: [{
          id: 1,
          nomeCliente: 'Cliente A',
          nomeProduto: 'Produto B',
          eventoId: 'evento-123',
          dataHoraEvento: '2026-06-25T12:00:00Z',
          salvoEm: '2026-06-25T12:01:00Z'
        }]
      }))
      .mockResolvedValueOnce(response({ pedidoId: 1, eventoId: 'evento-123', historico: [] }));
    vi.stubGlobal('fetch', fetchMock);
    await loadScript();
    await window.carregarEventos();

    expect(document.querySelectorAll('.evento-card')).toHaveLength(1);
    expect(document.getElementById('eventosList')?.textContent).toContain('Cliente A');
    (document.querySelector('.evento-card button') as HTMLButtonElement).click();

    await vi.waitFor(() => expect(fetchMock).toHaveBeenLastCalledWith(
      'http://localhost:8080/api/solicitacoes/1/historico',
      { method: 'GET', headers: { Accept: '*/*' } }
    ));
    expect((document.getElementById('pedidoId') as HTMLInputElement).value).toBe('1');
  });

  it('renderiza estado vazio ao listar pedidos', async () => {
    mockFetch({ eventos: [] });
    await loadScript();
    await window.carregarEventos();

    expect(document.querySelector('.empty-state')?.textContent).toContain('Nenhum pedido');
  });

  it('exibe erro devolvido ao listar pedidos', async () => {
    mockFetch({ erro: 'ServicoIndisponivel', mensagem: 'Banco temporariamente indisponível.' },
      { ok: false, status: 503, statusText: 'Service Unavailable' });
    await loadScript();
    await window.carregarEventos();

    expect(document.getElementById('eventosResponse')?.textContent).toContain('Banco temporariamente indisponível');
  });

  it('renderiza a linha do tempo completa e detalhes de erro', async () => {
    mockFetch({
      pedidoId: 42,
      eventoId: 'ES2-12345678-120000',
      historico: [
        { id: 1, status: 'Recebido', registradoEm: '2026-06-25T12:00:00Z', detalhe: null },
        { id: 2, status: 'Processando', registradoEm: '2026-06-25T12:00:01Z', detalhe: null },
        { id: 3, status: 'Erro', registradoEm: '2026-06-25T12:00:02Z', detalhe: 'Falha controlada' },
        { id: 4, status: 'Concluido', registradoEm: 'data-inválida', detalhe: null }
      ]
    });
    await loadScript();
    await window.consultarHistorico(new SubmitEvent('submit'));

    expect(document.querySelectorAll('.timeline-item')).toHaveLength(4);
    expect(document.querySelector('.status-erro')?.textContent).toContain('Falha controlada');
    expect(document.querySelector('.status-concluido')?.textContent).toContain('data-inválida');
    expect(document.getElementById('historicoList')?.textContent).toContain('ES2-12345678-120000');
  });

  it('valida o ID antes de consultar o histórico', async () => {
    const fetchMock = mockFetch({});
    await loadScript();
    await window.consultarHistoricoPorId(-5);

    expect(fetchMock).not.toHaveBeenCalled();
    expect(document.getElementById('historicoResponse')?.textContent).toContain('maior que zero');
  });

  it('exibe pedido não encontrado retornado pela API', async () => {
    mockFetch({ erro: 'PedidoNaoEncontrado', mensagem: 'Pedido 99 não encontrado.' },
      { ok: false, status: 404, statusText: 'Not Found' });
    await loadScript();
    await window.consultarHistoricoPorId(99);

    expect(document.getElementById('historicoResponse')?.classList.contains('error')).toBe(true);
    expect(document.getElementById('historicoResponse')?.textContent).toContain('Pedido 99 não encontrado');
  });

  it('trata interrupção de rede nas operações de pedido', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('rede interrompida')));
    await loadScript();

    await window.criarSolicitacao(new SubmitEvent('submit'));
    await window.carregarEventos();
    await window.consultarHistoricoPorId(42);

    expect(document.getElementById('solicitacaoResponse')?.textContent).toContain('rede interrompida');
    expect(document.getElementById('eventosResponse')?.textContent).toContain('rede interrompida');
    expect(document.getElementById('historicoResponse')?.textContent).toContain('rede interrompida');
  });
});

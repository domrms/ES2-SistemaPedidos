import { beforeEach, describe, expect, it, vi } from 'vitest';

const html = `
  <div id="healthResponse"></div>
  <div id="solicitacaoResponse"></div>
  <div id="eventosResponse"></div>
  <div id="eventosList"></div>
  <div id="healthStatus"><span class="status-indicator loading"></span></div>
  <span id="healthText">Verificando...</span>
  <form id="solicitacaoForm">
    <input id="clienteId" value="7" />
    <input id="produtoId" value="3" />
  </form>
`;

async function loadScript(): Promise<void> {
  vi.resetModules();
  document.body.innerHTML = html;
  await import('./script.ts');
}

function mockFetch(response: unknown, ok = true): ReturnType<typeof vi.fn> {
  const fetchMock = vi.fn().mockResolvedValue({
    ok,
    status: ok ? 200 : 400,
    statusText: ok ? 'OK' : 'Bad Request',
    json: vi.fn().mockResolvedValue(response)
  });

  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('frontend script', () => {
  beforeEach(() => {
    vi.unstubAllGlobals();
    vi.useFakeTimers();
  });

  it('verifica o healthcheck manualmente com sucesso', async () => {
    const fetchMock = mockFetch({ estado: 'healthy' });

    await loadScript();
    await window.checkHealth();

    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:8080/api/healthcheck',
      { method: 'GET', headers: { Accept: '*/*' } }
    );
    expect(document.getElementById('healthResponse')?.classList.contains('success')).toBe(true);
    expect(document.getElementById('healthResponse')?.textContent).toContain('API está saudável!');
  });

  it('cria uma solicitacao e limpa o formulario', async () => {
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
  });

  it('renderiza eventos retornados pela API', async () => {
    mockFetch({
      eventos: [
        {
          id: 1,
          nomeCliente: 'Cliente A',
          nomeProduto: 'Produto B',
          eventoId: 'evento-123',
          dataHoraEvento: '2026-06-25T12:00:00Z',
          salvoEm: '2026-06-25T12:01:00Z'
        }
      ]
    });

    await loadScript();
    await window.carregarEventos();

    expect(document.querySelectorAll('.evento-card')).toHaveLength(1);
    expect(document.getElementById('eventosList')?.textContent).toContain('Cliente A');
    expect(document.getElementById('eventosResponse')?.textContent).toContain('1 evento(s)');
  });

  it('mostra erro quando a API falha', async () => {
    const fetchMock = vi.fn().mockRejectedValue(new Error('API indisponivel'));
    vi.stubGlobal('fetch', fetchMock);

    await loadScript();
    await window.checkHealth();

    expect(document.getElementById('healthResponse')?.classList.contains('error')).toBe(true);
    expect(document.getElementById('healthResponse')?.textContent).toContain('API indisponivel');
  });
});

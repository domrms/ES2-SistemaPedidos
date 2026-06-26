const API_BASE_URL = 'http://localhost:8080/api';

interface HealthResponse {
    status?: string;
    estado?: string;
}

interface ApiErrorResponse {
    message?: string;
    mensagem?: string;
}

interface Evento {
    id: number;
    nomeCliente: string;
    nomeProduto: string;
    eventoId: string;
    dataHoraEvento: string;
    salvoEm: string;
}

interface EventosResponse extends ApiErrorResponse {
    eventos?: Evento[];
}

interface Window {
    checkHealth: () => Promise<void>;
    criarSolicitacao: (event: SubmitEvent) => Promise<void>;
    carregarEventos: () => Promise<void>;
}

function getElementById<T extends HTMLElement>(id: string): T {
    const element = document.getElementById(id);

    if (!element) {
        throw new Error(`Elemento #${id} nao encontrado.`);
    }

    return element as T;
}

const healthResponse = getElementById<HTMLDivElement>('healthResponse');
const solicitacaoResponse = getElementById<HTMLDivElement>('solicitacaoResponse');
const eventosResponse = getElementById<HTMLDivElement>('eventosResponse');
const eventosList = getElementById<HTMLDivElement>('eventosList');
const healthStatus = getElementById<HTMLDivElement>('healthStatus');
const healthText = getElementById<HTMLSpanElement>('healthText');

document.addEventListener('DOMContentLoaded', () => {
    checkHealthOnLoad();
    setInterval(checkHealthOnLoad, 30000);
});

async function checkHealthOnLoad(): Promise<void> {
    try {
        const response = await fetch(`${API_BASE_URL}/healthcheck`, {
            method: 'GET',
            headers: { 'Accept': '*/*' }
        });

        if (response.ok) {
            const data = await response.json() as HealthResponse;
            updateHealthStatus(true, data.status || data.estado || 'Online');
        } else {
            updateHealthStatus(false, 'Offline');
        }
    } catch {
        updateHealthStatus(false, 'Desconectado');
    }
}

function updateHealthStatus(isHealthy: boolean, message: string): void {
    const indicator = healthStatus.querySelector<HTMLElement>('.status-indicator');

    if (!indicator) {
        throw new Error('Indicador de status nao encontrado.');
    }

    indicator.className = 'status-indicator';

    if (isHealthy) {
        indicator.classList.add('success');
        healthText.textContent = `✅ ${message}`;
        healthText.style.color = '#10b981';
    } else {
        indicator.classList.add('error');
        healthText.textContent = `❌ ${message}`;
        healthText.style.color = '#ef4444';
    }
}

async function checkHealth(): Promise<void> {
    clearResponse(healthResponse);
    showLoadingMessage(healthResponse, 'Verificando saúde da API...');

    try {
        const response = await fetch(`${API_BASE_URL}/healthcheck`, {
            method: 'GET',
            headers: { 'Accept': '*/*' }
        });

        if (response.ok) {
            const data = await response.json() as HealthResponse;
            showSuccessResponse(healthResponse, 'API está saudável!', data);
        } else {
            showErrorResponse(healthResponse, `Erro: ${response.status} ${response.statusText}`);
        }
    } catch (error) {
        showErrorResponse(healthResponse, `Erro de conexão: ${getErrorMessage(error)}`);
    }
}

async function criarSolicitacao(event: SubmitEvent): Promise<void> {
    event.preventDefault();
    clearResponse(solicitacaoResponse);

    const clienteId = getElementById<HTMLInputElement>('clienteId').value;
    const produtoId = getElementById<HTMLInputElement>('produtoId').value;

    showLoadingMessage(solicitacaoResponse, 'Enviando solicitação...');

    try {
        const response = await fetch(`${API_BASE_URL}/solicitacoes`, {
            method: 'POST',
            headers: {
                'Accept': '*/*',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                clienteId: parseInt(clienteId, 10),
                produtoId: parseInt(produtoId, 10)
            })
        });

        const data = await response.json() as ApiErrorResponse;

        if (response.ok) {
            showSuccessResponse(
                solicitacaoResponse,
                '✅ Solicitação criada com sucesso!',
                data
            );
            getElementById<HTMLFormElement>('solicitacaoForm').reset();
        } else {
            showErrorResponse(
                solicitacaoResponse,
                `❌ Erro ao criar solicitação: ${data.message || data.mensagem || response.statusText}`,
                data
            );
        }
    } catch (error) {
        showErrorResponse(
            solicitacaoResponse,
            `❌ Erro de conexão: ${getErrorMessage(error)}`
        );
    }
}

async function carregarEventos(): Promise<void> {
    clearResponse(eventosResponse);
    eventosList.innerHTML = '';
    showLoadingMessage(eventosResponse, 'Carregando eventos...');

    try {
        const response = await fetch(`${API_BASE_URL}/solicitacoes/eventos`, {
            method: 'GET',
            headers: { 'Accept': '*/*' }
        });

        const data = await response.json() as EventosResponse;

        if (response.ok && data.eventos) {
            const eventos = data.eventos;
            showInfoResponse(
                eventosResponse,
                `📊 ${eventos.length} evento(s) encontrado(s)`
            );
            renderizarEventos(eventos);
        } else {
            showErrorResponse(
                eventosResponse,
                `❌ Erro ao carregar eventos: ${data.message || data.mensagem || response.statusText}`,
                data
            );
        }
    } catch (error) {
        showErrorResponse(
            eventosResponse,
            `❌ Erro de conexão: ${getErrorMessage(error)}`
        );
    }
}

function renderizarEventos(eventos: Evento[]): void {
    eventosList.innerHTML = '';

    if (eventos.length === 0) {
        eventosList.innerHTML = '<p style="grid-column: 1/-1; text-align: center; color: var(--text-light);">Nenhum evento registrado.</p>';
        return;
    }

    eventos.forEach((evento) => {
        const card = document.createElement('div');
        card.className = 'evento-card';

        card.innerHTML = `
            <h3>📌 Evento #${evento.id}</h3>

            <div class="evento-campo">
                <strong>Cliente:</strong>
                <span>${evento.nomeCliente}</span>
            </div>

            <div class="evento-campo">
                <strong>Produto:</strong>
                <span>${evento.nomeProduto}</span>
            </div>

            <div class="evento-campo">
                <strong>ID do Evento:</strong>
                <div style="width: 100%; text-align: right;">
                    <span class="evento-id">${evento.eventoId}</span>
                </div>
            </div>

            <div class="evento-campo">
                <strong>Data/Hora Evento:</strong>
                <span>${formatarData(evento.dataHoraEvento)}</span>
            </div>

            <div class="evento-campo">
                <strong>Salvo em:</strong>
                <span>${formatarData(evento.salvoEm)}</span>
            </div>
        `;

        eventosList.appendChild(card);
    });
}

function showLoadingMessage(element: HTMLElement, message: string): void {
    element.classList.add('show', 'info');
    element.innerHTML = `<span class="spinner"></span>${message}`;
}

function showSuccessResponse(element: HTMLElement, message: string, data?: object): void {
    element.classList.remove('error', 'info');
    element.classList.add('show', 'success');

    let html = `<strong>${message}</strong>`;
    if (data && Object.keys(data).length > 0) {
        html += `<code>${JSON.stringify(data, null, 2)}</code>`;
    }

    element.innerHTML = html;
}

function showErrorResponse(element: HTMLElement, message: string, data: object | null = null): void {
    element.classList.remove('success', 'info');
    element.classList.add('show', 'error');

    let html = `<strong>${message}</strong>`;
    if (data && Object.keys(data).length > 0) {
        html += `<code>${JSON.stringify(data, null, 2)}</code>`;
    }

    element.innerHTML = html;
}

function showInfoResponse(element: HTMLElement, message: string): void {
    element.classList.remove('error', 'success');
    element.classList.add('show', 'info');
    element.innerHTML = `<strong>${message}</strong>`;
}

function clearResponse(element: HTMLElement): void {
    element.classList.remove('show', 'success', 'error', 'info');
    element.innerHTML = '';
}

function formatarData(dataString: string): string {
    try {
        const data = new Date(dataString);
        return data.toLocaleString('pt-BR', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });
    } catch {
        return dataString;
    }
}

function getErrorMessage(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
}

window.checkHealth = checkHealth;
window.criarSolicitacao = criarSolicitacao;
window.carregarEventos = carregarEventos;

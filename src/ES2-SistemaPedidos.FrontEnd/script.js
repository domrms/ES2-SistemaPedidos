"use strict";
const API_BASE_URL = 'http://localhost:8080/api';
function getElementById(id) {
    const element = document.getElementById(id);
    if (!element)
        throw new Error(`Elemento #${id} não encontrado.`);
    return element;
}
const healthResponse = getElementById('healthResponse');
const solicitacaoResponse = getElementById('solicitacaoResponse');
const eventosResponse = getElementById('eventosResponse');
const eventosList = getElementById('eventosList');
const historicoResponse = getElementById('historicoResponse');
const historicoList = getElementById('historicoList');
const healthStatus = getElementById('healthStatus');
const healthText = getElementById('healthText');
document.addEventListener('DOMContentLoaded', () => {
    void checkHealthOnLoad();
    setInterval(() => void checkHealthOnLoad(), 30000);
});
async function obterHealth() {
    const response = await fetch(`${API_BASE_URL}/healthcheck`, {
        method: 'GET',
        headers: { 'Accept': '*/*' }
    });
    return { response, data: await response.json() };
}
async function checkHealthOnLoad() {
    try {
        const { response, data } = await obterHealth();
        const saudavel = response.ok && data.estado === 'healthy';
        updateHealthStatus(saudavel, saudavel ? 'Serviços operacionais' : traduzirEstadoHealth(data.estado));
    }
    catch {
        updateHealthStatus(false, 'Desconectado');
    }
}
function updateHealthStatus(isHealthy, message) {
    const indicator = healthStatus.querySelector('.status-indicator');
    if (!indicator)
        throw new Error('Indicador de status não encontrado.');
    indicator.className = `status-indicator ${isHealthy ? 'success' : 'error'}`;
    healthText.textContent = `${isHealthy ? '✅' : '❌'} ${message}`;
    healthText.style.color = isHealthy ? '#10b981' : '#ef4444';
}
async function checkHealth() {
    clearResponse(healthResponse);
    showLoadingMessage(healthResponse, 'Verificando saúde da API e dependências...');
    try {
        const { response, data } = await obterHealth();
        if (response.ok && data.estado === 'healthy') {
            showSuccessResponse(healthResponse, 'API e dependências estão saudáveis!');
        }
        else {
            showErrorResponse(healthResponse, `Saúde do sistema: ${traduzirEstadoHealth(data.estado)}`);
        }
        renderizarDiagnosticosHealth(healthResponse, data);
    }
    catch (error) {
        showErrorResponse(healthResponse, `Erro de conexão: ${getErrorMessage(error)}`);
    }
}
function renderizarDiagnosticosHealth(element, data) {
    const verificacoes = Object.entries(data.verificacoes ?? {});
    if (verificacoes.length === 0)
        return;
    const lista = document.createElement('div');
    lista.className = 'health-details';
    verificacoes.forEach(([nome, diagnostico]) => {
        const item = document.createElement('div');
        item.className = `health-detail ${diagnostico.estado === 'healthy' ? 'healthy' : 'unhealthy'}`;
        const titulo = document.createElement('strong');
        titulo.textContent = `${diagnostico.estado === 'healthy' ? '✓' : '✕'} ${formatarNomeServico(nome)}`;
        const descricao = document.createElement('span');
        descricao.textContent = diagnostico.descricao ?? traduzirEstadoHealth(diagnostico.estado);
        item.append(titulo, descricao);
        lista.appendChild(item);
    });
    element.appendChild(lista);
}
async function criarSolicitacao(event) {
    event.preventDefault();
    clearResponse(solicitacaoResponse);
    const clienteId = Number(getElementById('clienteId').value);
    const produtoId = Number(getElementById('produtoId').value);
    if (!Number.isInteger(clienteId) || clienteId <= 0 || !Number.isInteger(produtoId) || produtoId <= 0) {
        showErrorResponse(solicitacaoResponse, 'Cliente e produto devem possuir IDs inteiros maiores que zero.');
        return;
    }
    showLoadingMessage(solicitacaoResponse, 'Enviando solicitação...');
    try {
        const response = await fetch(`${API_BASE_URL}/solicitacoes`, {
            method: 'POST',
            headers: { 'Accept': '*/*', 'Content-Type': 'application/json' },
            body: JSON.stringify({ clienteId, produtoId })
        });
        const data = await response.json();
        if (response.ok) {
            showSuccessResponse(solicitacaoResponse, 'Solicitação recebida para processamento!', data);
            getElementById('solicitacaoForm').reset();
        }
        else {
            showErrorResponse(solicitacaoResponse, `Erro ao criar solicitação: ${obterMensagemErro(data, response)}`, data);
        }
    }
    catch (error) {
        showErrorResponse(solicitacaoResponse, `Erro de conexão: ${getErrorMessage(error)}`);
    }
}
async function carregarEventos() {
    clearResponse(eventosResponse);
    eventosList.replaceChildren();
    showLoadingMessage(eventosResponse, 'Carregando pedidos processados...');
    try {
        const response = await fetch(`${API_BASE_URL}/solicitacoes/eventos`, {
            method: 'GET',
            headers: { 'Accept': '*/*' }
        });
        const data = await response.json();
        if (response.ok && Array.isArray(data.eventos)) {
            showInfoResponse(eventosResponse, `${data.eventos.length} pedido(s) encontrado(s)`);
            renderizarEventos(data.eventos);
        }
        else {
            showErrorResponse(eventosResponse, `Erro ao carregar pedidos: ${obterMensagemErro(data, response)}`, data);
        }
    }
    catch (error) {
        showErrorResponse(eventosResponse, `Erro de conexão: ${getErrorMessage(error)}`);
    }
}
function renderizarEventos(eventos) {
    eventosList.replaceChildren();
    if (eventos.length === 0) {
        const vazio = document.createElement('p');
        vazio.className = 'empty-state';
        vazio.textContent = 'Nenhum pedido processado.';
        eventosList.appendChild(vazio);
        return;
    }
    eventos.forEach(evento => {
        const card = document.createElement('article');
        card.className = 'evento-card';
        adicionarTexto(card, 'h3', `Pedido #${evento.id}`);
        adicionarCampo(card, 'Cliente:', evento.nomeCliente);
        adicionarCampo(card, 'Produto:', evento.nomeProduto);
        adicionarCampo(card, 'ID do evento:', evento.eventoId, 'evento-id');
        adicionarCampo(card, 'Recebido em:', formatarData(evento.dataHoraEvento));
        adicionarCampo(card, 'Persistido em:', formatarData(evento.salvoEm));
        const botao = document.createElement('button');
        botao.type = 'button';
        botao.className = 'btn btn-secondary btn-small';
        botao.textContent = 'Ver histórico';
        botao.addEventListener('click', () => void consultarHistoricoPorId(evento.id));
        card.appendChild(botao);
        eventosList.appendChild(card);
    });
}
async function consultarHistorico(event) {
    event.preventDefault();
    const pedidoId = Number(getElementById('pedidoId').value);
    await consultarHistoricoPorId(pedidoId);
}
async function consultarHistoricoPorId(pedidoId) {
    clearResponse(historicoResponse);
    historicoList.replaceChildren();
    if (!Number.isInteger(pedidoId) || pedidoId <= 0) {
        showErrorResponse(historicoResponse, 'O ID do pedido deve ser um número inteiro maior que zero.');
        return;
    }
    getElementById('pedidoId').value = String(pedidoId);
    showLoadingMessage(historicoResponse, `Carregando histórico do pedido #${pedidoId}...`);
    try {
        const response = await fetch(`${API_BASE_URL}/solicitacoes/${pedidoId}/historico`, {
            method: 'GET',
            headers: { 'Accept': '*/*' }
        });
        const data = await response.json();
        if (response.ok && Array.isArray(data.historico)) {
            showSuccessResponse(historicoResponse, `Histórico do pedido #${data.pedidoId ?? pedidoId}`);
            renderizarHistorico(data);
        }
        else {
            showErrorResponse(historicoResponse, obterMensagemErro(data, response), data);
        }
    }
    catch (error) {
        showErrorResponse(historicoResponse, `Erro de conexão: ${getErrorMessage(error)}`);
    }
}
function renderizarHistorico(data) {
    adicionarTexto(historicoList, 'p', `Evento: ${data.eventoId ?? 'não informado'}`, 'historico-evento-id');
    const historico = data.historico ?? [];
    if (historico.length === 0) {
        adicionarTexto(historicoList, 'p', 'Este pedido ainda não possui transições registradas.', 'empty-state');
        return;
    }
    const timeline = document.createElement('ol');
    timeline.className = 'timeline';
    historico.forEach(transicao => {
        const item = document.createElement('li');
        item.className = `timeline-item status-${classeStatus(transicao.status)}`;
        adicionarTexto(item, 'strong', transicao.status, 'timeline-status');
        adicionarTexto(item, 'time', formatarData(transicao.registradoEm), 'timeline-date');
        if (transicao.detalhe)
            adicionarTexto(item, 'p', transicao.detalhe, 'timeline-detail');
        timeline.appendChild(item);
    });
    historicoList.appendChild(timeline);
}
function adicionarCampo(container, rotulo, valor, classeValor) {
    const campo = document.createElement('div');
    campo.className = 'evento-campo';
    adicionarTexto(campo, 'strong', rotulo);
    adicionarTexto(campo, 'span', valor, classeValor);
    container.appendChild(campo);
}
function adicionarTexto(container, tag, texto, classe) {
    const elemento = document.createElement(tag);
    elemento.textContent = texto;
    if (classe)
        elemento.className = classe;
    container.appendChild(elemento);
    return elemento;
}
function showLoadingMessage(element, message) {
    element.className = 'response-area show info';
    const spinner = document.createElement('span');
    spinner.className = 'spinner';
    element.replaceChildren(spinner, document.createTextNode(message));
}
function showSuccessResponse(element, message, data) {
    element.className = 'response-area show success';
    element.replaceChildren();
    adicionarTexto(element, 'strong', message);
    if (data && Object.keys(data).length > 0)
        adicionarJson(element, data);
}
function showErrorResponse(element, message, data = null) {
    element.className = 'response-area show error';
    element.replaceChildren();
    adicionarTexto(element, 'strong', message);
    if (data && Object.keys(data).length > 0)
        adicionarJson(element, data);
}
function showInfoResponse(element, message) {
    element.className = 'response-area show info';
    element.replaceChildren();
    adicionarTexto(element, 'strong', message);
}
function adicionarJson(element, data) {
    adicionarTexto(element, 'code', JSON.stringify(data, null, 2));
}
function clearResponse(element) {
    element.className = 'response-area';
    element.replaceChildren();
}
function formatarData(dataString) {
    const data = new Date(dataString);
    if (Number.isNaN(data.getTime()))
        return dataString;
    return data.toLocaleString('pt-BR', {
        year: 'numeric', month: '2-digit', day: '2-digit',
        hour: '2-digit', minute: '2-digit', second: '2-digit'
    });
}
function classeStatus(status) {
    return ({ Recebido: 'recebido', Processando: 'processando', Concluido: 'concluido', Erro: 'erro' })[status];
}
function traduzirEstadoHealth(estado) {
    return { healthy: 'Saudável', degraded: 'Degradado', unhealthy: 'Indisponível' }[estado ?? 'unhealthy'];
}
function formatarNomeServico(nome) {
    return { postgresql: 'PostgreSQL', floci: 'Floci' }[nome] ?? nome;
}
function obterMensagemErro(data, response) {
    return data.mensagem || data.message || data.erro || `${response.status} ${response.statusText}`;
}
function getErrorMessage(error) {
    return error instanceof Error ? error.message : String(error);
}
window.checkHealth = checkHealth;
window.criarSolicitacao = criarSolicitacao;
window.carregarEventos = carregarEventos;
window.consultarHistorico = consultarHistorico;
window.consultarHistoricoPorId = consultarHistoricoPorId;

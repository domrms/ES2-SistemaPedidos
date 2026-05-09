// Configuração da API
const API_BASE_URL = 'http://localhost:8080/api';

// Elementos do DOM
const healthResponse = document.getElementById('healthResponse');
const solicitacaoResponse = document.getElementById('solicitacaoResponse');
const eventosResponse = document.getElementById('eventosResponse');
const eventosList = document.getElementById('eventosList');
const healthStatus = document.getElementById('healthStatus');
const healthText = document.getElementById('healthText');

// Inicializar ao carregar a página
document.addEventListener('DOMContentLoaded', () => {
    checkHealthOnLoad();
    // Verifica saúde a cada 30 segundos
    setInterval(checkHealthOnLoad, 30000);
});

// ============= HEALTH CHECK =============
async function checkHealthOnLoad() {
    try {
        const response = await fetch(`${API_BASE_URL}/healthcheck`, {
            method: 'GET',
            headers: { 'Accept': '*/*' }
        });

        if (response.ok) {
            const data = await response.json();
            updateHealthStatus(true, data.status || 'Online');
        } else {
            updateHealthStatus(false, 'Offline');
        }
    } catch (error) {
        updateHealthStatus(false, 'Desconectado');
    }
}

function updateHealthStatus(isHealthy, message) {
    const indicator = healthStatus.querySelector('.status-indicator');
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

async function checkHealth() {
    clearResponse(healthResponse);
    showLoadingMessage(healthResponse, 'Verificando saúde da API...');

    try {
        const response = await fetch(`${API_BASE_URL}/healthcheck`, {
            method: 'GET',
            headers: { 'Accept': '*/*' }
        });

        if (response.ok) {
            const data = await response.json();
            showSuccessResponse(healthResponse, 'API está saudável!', data);
        } else {
            showErrorResponse(healthResponse, `Erro: ${response.status} ${response.statusText}`);
        }
    } catch (error) {
        showErrorResponse(healthResponse, `Erro de conexão: ${error.message}`);
    }
}

// ============= CRIAR SOLICITAÇÃO =============
async function criarSolicitacao(event) {
    event.preventDefault();
    clearResponse(solicitacaoResponse);

    const clienteId = document.getElementById('clienteId').value;
    const produtoId = document.getElementById('produtoId').value;

    showLoadingMessage(solicitacaoResponse, 'Enviando solicitação...');

    try {
        const response = await fetch(`${API_BASE_URL}/solicitacoes`, {
            method: 'POST',
            headers: {
                'Accept': '*/*',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                clienteId: parseInt(clienteId),
                produtoId: parseInt(produtoId)
            })
        });

        const data = await response.json();

        if (response.ok) {
            showSuccessResponse(
                solicitacaoResponse,
                '✅ Solicitação criada com sucesso!',
                data
            );
            document.getElementById('solicitacaoForm').reset();
        } else {
            showErrorResponse(
                solicitacaoResponse,
                `❌ Erro ao criar solicitação: ${data.message || response.statusText}`,
                data
            );
        }
    } catch (error) {
        showErrorResponse(
            solicitacaoResponse,
            `❌ Erro de conexão: ${error.message}`
        );
    }
}

// ============= CARREGAR EVENTOS =============
async function carregarEventos() {
    clearResponse(eventosResponse);
    eventosList.innerHTML = '';
    showLoadingMessage(eventosResponse, 'Carregando eventos...');

    try {
        const response = await fetch(`${API_BASE_URL}/solicitacoes/eventos`, {
            method: 'GET',
            headers: { 'Accept': '*/*' }
        });

        const data = await response.json();

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
                `❌ Erro ao carregar eventos: ${data.message || response.statusText}`,
                data
            );
        }
    } catch (error) {
        showErrorResponse(
            eventosResponse,
            `❌ Erro de conexão: ${error.message}`
        );
    }
}

function renderizarEventos(eventos) {
    eventosList.innerHTML = '';

    if (eventos.length === 0) {
        eventosList.innerHTML = '<p style="grid-column: 1/-1; text-align: center; color: var(--text-light);">Nenhum evento registrado.</p>';
        return;
    }

    eventos.forEach((evento, index) => {
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

// ============= FUNÇÕES AUXILIARES =============
function showLoadingMessage(element, message) {
    element.classList.add('show', 'info');
    element.innerHTML = `<span class="spinner"></span>${message}`;
}

function showSuccessResponse(element, message, data) {
    element.classList.remove('error', 'info');
    element.classList.add('show', 'success');
    
    let html = `<strong>${message}</strong>`;
    if (data && Object.keys(data).length > 0) {
        html += `<code>${JSON.stringify(data, null, 2)}</code>`;
    }
    
    element.innerHTML = html;
}

function showErrorResponse(element, message, data = null) {
    element.classList.remove('success', 'info');
    element.classList.add('show', 'error');
    
    let html = `<strong>${message}</strong>`;
    if (data && Object.keys(data).length > 0) {
        html += `<code>${JSON.stringify(data, null, 2)}</code>`;
    }
    
    element.innerHTML = html;
}

function showInfoResponse(element, message) {
    element.classList.remove('error', 'success');
    element.classList.add('show', 'info');
    element.innerHTML = `<strong>${message}</strong>`;
}

function clearResponse(element) {
    element.classList.remove('show', 'success', 'error', 'info');
    element.innerHTML = '';
}

function formatarData(dataString) {
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
    } catch (error) {
        return dataString;
    }
}

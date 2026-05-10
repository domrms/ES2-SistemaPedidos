# Frontend - Sistema de Pedidos ES2

Frontend simples e moderno para gerenciamento de solicitações e eventos do sistema ES2.

## 📋 Funcionalidades

- ✅ **Health Check**: Verificar o status da API em tempo real
- ➕ **Nova Solicitação**: Criar novas solicitações fornecendo ID do cliente e ID do produto
- 📊 **Listar Eventos**: Visualizar todos os eventos registrados no sistema com informações detalhadas
- 🎨 **Interface Bonita**: Design moderno e responsivo

## 🚀 Como Usar

### Opção 1: Abrir direto no navegador

1. Abra o arquivo `index.html` no navegador
2. Configure a URL da API se necessário (padrão: `http://localhost:5000`)

### Opção 2: Usar um servidor local (recomendado)

```bash
# Python 3
python -m http.server 8000

# Node.js (http-server)
npx http-server

# PowerShell
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add('http://localhost:8000/')
$listener.Start()
```

Depois acesse: `http://localhost:8000`

## 🔧 Funcionalidades Detalhadas

### Health Check
- **GET /api/healthcheck**: Verifica se a API está online
- Status é monitorado automaticamente a cada 30 segundos
- Indicador visual na header mostra conexão em tempo real

### Nova Solicitação
- **POST /api/solicitacoes**
- Aceita:
  - `clienteId` (número): ID do cliente
  - `produtoId` (número): ID do produto
- Resposta mostra sucesso ou erro com detalhes

### Listar Eventos
- **GET /api/solicitacoes/eventos**
- Exibe todos os eventos em cards informativos
- Cada evento mostra:
  - ID do evento
  - Nome do cliente
  - Nome do produto
  - Evento ID único
  - Data/hora do evento
  - Data/hora de registro no sistema

## 📡 Tratamento de Erros

O frontend exibe mensagens de erro e sucesso de forma clara:

- **Verde**: Operação bem-sucedida
- **Vermelho**: Erro na operação
- **Azul**: Informações gerais
- **Amarelo**: Carregamento em progresso

## 🎨 Personalização

### Alterar URL da API

Edite `script.js` na linha:
```javascript
const API_BASE_URL = 'http://localhost:5000/api';
```

### Cores

As cores estão definidas em `styles.css` nas variáveis CSS:
```css
--primary-color: #3b82f6;
--success-color: #10b981;
--error-color: #ef4444;
```

## 📱 Responsividade

O frontend é totalmente responsivo e funciona em:
- Desktop
- Tablet
- Mobile

## 🛠️ Tecnologias

- HTML5 semântico
- CSS3 com variáveis e gradientes
- JavaScript vanilla (sem dependências)
- Fetch API para requisições HTTP
- JSON para comunicação com API

## 📝 Notas

- A API precisa estar rodando em `http://localhost:5000` (configure no script.js se necessário)
- CORS precisa estar habilitado na API para aceitar requisições do frontend
- O frontend faz requisições em tempo real, sem cache

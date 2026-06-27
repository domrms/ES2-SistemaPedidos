# ES2 - Sistema de Pedidos

Projeto demonstrativo de um sistema de pedidos com API HTTP, processamento assincrono via SQS/Lambda, persistencia em PostgreSQL, frontend estatico e testes unitarios/E2E.

O fluxo principal e:

1. A API recebe uma solicitacao de cliente/produto.
2. A API valida cliente e produto no banco.
3. A API publica um evento na fila SQS local.
4. A Lambda consumidora processa a mensagem.
5. A Lambda grava o evento na tabela `eventos`.
6. A API permite consultar os eventos registrados.

## Tecnologias

- .NET 10 para a API e testes unitarios principais.
- .NET 8 para a Lambda e testes E2E.
- ASP.NET Core Web API.
- PostgreSQL 15.
- Entity Framework Core/Npgsql no projeto compartilhado.
- Dapper/Npgsql no consumer Lambda.
- Amazon SQS SDK e eventos Lambda SQS.
- Serilog para logs.
- Swagger/OpenAPI.
- Docker Compose.
- Floci como emulador AWS local na porta `4566`.
- xUnit para testes unitarios.
- Reqnroll para cenarios E2E em Gherkin.
- Frontend HTML/CSS/JavaScript vanilla.

## Estrutura de Pastas

```text
.
|-- .github/
|   |-- agents/                 # Agentes auxiliares do Spec Kit/Copilot
|   |-- prompts/                # Prompts auxiliares do Spec Kit/Copilot
|   `-- copilot-instructions.md # Instrucoes para assistentes de codigo
|-- .run/                       # Configuracoes locais de execucao da IDE
|-- .vscode/                    # Configuracoes do VS Code
|-- database/
|   |-- 01-ddl.sql              # Cria tabelas, chaves e indices
|   `-- 02-dml.sql              # Carga inicial de clientes e produtos
|-- docker/
|   |-- aws/init-aws.sh         # Provisiona SQS, DLQ, Lambda e trigger local
|   |-- data/                   # Estado local persistido do emulador AWS
|   |-- docker-compose.yml      # Orquestracao local completa
|   |-- Dockerfile.api          # Build da API
|   `-- Dockerfile.lambda       # Build da Lambda consumidora
|-- src/
|   |-- ES2-SistemaPedidos.Api/
|   |-- ES2-SistemaPedidos.FrontEnd/
|   |-- ES2-SistemaPedidos.LambdaConsumerSQS/
|   `-- ES2-SistemaPedidos.Shared/
|-- tests/
|   |-- e2e/                    # Testes E2E Reqnroll/xUnit
|   |-- integration/            # Pasta reservada para testes de integracao
|   `-- unit/                   # Testes unitarios
|-- dotnet-tools.json           # Ferramentas locais .NET
|-- ES2-SistemaPedidos.sln      # Solucao .NET
`-- readme.md
```

## Projetos da Solucao

### `src/ES2-SistemaPedidos.Api`

API ASP.NET Core responsavel por expor os endpoints HTTP, validar solicitacoes e publicar eventos na fila SQS.

Principais pastas:

- `Application/Abstractions`: contratos internos da camada de aplicacao, como o publicador de eventos.
- `Application/Pedidos`: servico de negocio `PedidoService`.
- `Controllers`: controllers HTTP.
- `Infrastructure/Messaging`: implementacao SQS do publicador de eventos.
- `Models`: contratos de entrada/saida da API e tipo `Resultado<T>`.
- `Properties/launchSettings.json`: perfil local da API em `http://localhost:5000`.

Funcionalidades:

- Health check da API.
- Criacao de solicitacao de pedido.
- Validacao de `clienteId` e `produtoId`.
- Validacao de existencia de cliente/produto no banco.
- Geracao de `eventoId` no formato `ES2-00000000-HHmmss`.
- Conversao de data/hora para fuso de Brasilia.
- Publicacao do evento no SQS.
- Consulta de eventos gravados.
- Tratamento de falhas de banco/mensageria com HTTP `503`.
- Swagger habilitado.
- CORS liberado para qualquer origem.

### `src/ES2-SistemaPedidos.LambdaConsumerSQS`

Aplicacao Lambda responsavel por consumir mensagens SQS e persistir eventos no banco.

Principais pastas:

- `Application/Abstractions`: contrato `IPedidoProcessamentoRepository`.
- `Application/Models`: modelo interno `EventoProcessamento`.
- `Application/Services`: servico `ProcessadorPedidoService`.
- `Infrastructure/Data`: repositorio Dapper/Npgsql.
- `Function.cs`: handler Lambda para `SQSEvent`.
- `Program.cs`: ponto de entrada console informativo.
- `aws-lambda-tools-defaults.json`: configuracao da Lambda para tooling local/AWS.

Funcionalidades:

- Desserializa mensagens no contrato compartilhado `EventoSolicitacaoCliente`.
- Valida payload recebido.
- Persiste eventos na tabela `eventos`.
- Usa `ON CONFLICT (evento_id) DO NOTHING` para idempotencia.
- Retorna `SQSBatchResponse` com falhas por mensagem.
- Suporta `ReportBatchItemFailures` no event source mapping.

Handler:

```text
ES2-SistemaPedidos.LambdaConsumerSQS::ES2_SistemaPedidos.LambdaConsumerSQS.Function::FunctionHandler
```

### `src/ES2-SistemaPedidos.Shared`

Biblioteca compartilhada entre API, Lambda e testes.

Principais pastas:

- `Contracts`: contrato de evento publicado na fila.
- `Data`: `ApplicationDbContext` e configuracao EF Core.
- `Data/Repository`: repositorios de consulta para cliente, produto e eventos.
- `Domain`: entidades `Cliente`, `Produto` e `EventoCliente`.
- `Domain/Repositories`: interfaces dos repositorios.
- `Logging`: formatter de console com data/hora em Brasilia.

Funcionalidades:

- Mapeamento das tabelas `clientes`, `produtos` e `eventos`.
- Injecao de dependencias de persistencia com `AddPersistenciaPedidos`.
- Repositorios para validar cliente/produto e listar eventos detalhados.
- Contrato compartilhado da mensagem SQS.

### `src/ES2-SistemaPedidos.FrontEnd`

Frontend estatico em HTML/CSS/JavaScript.

Arquivos:

- `index.html`: tela principal.
- `styles.css`: estilos responsivos.
- `script.js`: chamadas HTTP para a API.
- `README.md`: documentacao especifica do frontend.

Funcionalidades:

- Verificar health check da API.
- Criar solicitacao informando `clienteId` e `produtoId`.
- Listar eventos registrados.
- Exibir mensagens de sucesso, erro e carregamento.
- Monitorar a API automaticamente a cada 30 segundos.

Por padrao o frontend usa:

```javascript
const API_BASE_URL = 'http://localhost:8080/api';
```

## Banco de Dados

O banco padrao e PostgreSQL com database `es2_pedidos`, usuario `dev` e senha `dev`.

Tabelas:

- `clientes`: cadastro basico de clientes.
- `produtos`: cadastro basico de produtos.
- `eventos`: eventos processados pela Lambda.
- `pedido_status`: historico append-only das transicoes de cada pedido.

Scripts:

- `database/01-ddl.sql`: cria tabelas, chaves estrangeiras, constraint unica de `evento_id` e indice por cliente/produto/data.
- `database/02-dml.sql`: insere clientes e produtos de exemplo:
  - Clientes `1` a `4`.
  - Produtos `1` a `4`.

Dados E2E:

- Cliente de teste: `9999`, nome `Cliente E2E Test`.
- Produto de teste: `9999`, nome `Produto E2E Test`.
- IDs inexistentes usados nos testes: cliente `9998` e produto `9998`.

## Endpoints da API

Base URL no Docker Compose:

```text
http://localhost:8080
```

Base URL no perfil local do projeto:

```text
http://localhost:5000
```

### `GET /api/healthcheck`

Verifica ativamente a conectividade com PostgreSQL e Floci. Retorna `503 Service Unavailable` se uma dependencia
estiver indisponivel.

Resposta `200 OK`:

```json
{
  "estado": "healthy",
  "duracao": "00:00:00.0123456",
  "verificacoes": {
    "postgresql": {
      "estado": "healthy",
      "descricao": "Conexao com PostgreSQL disponivel."
    },
    "floci": {
      "estado": "healthy",
      "descricao": "Floci acessivel (HTTP 200)."
    }
  }
}
```

### `POST /api/solicitacoes`

Cria uma solicitacao e publica um evento na fila SQS.

Request:

```json
{
  "clienteId": 1,
  "produtoId": 1
}
```

Resposta `202 Accepted`:

```json
{
  "clienteId": 1,
  "produtoId": 1,
  "eventoId": "ES2-12345678-153015",
  "dataHoraRequisicao": "2026-05-10T15:30:15-03:00"
}
```

Resposta `400 Bad Request` para validacao:

```json
{
  "erro": "ValidacaoFalhou",
  "mensagem": "A validacao da solicitacao falhou",
  "detalhes": [
    {
      "campo": "clienteId",
      "erro": "Cliente 9998 nao encontrado."
    }
  ]
}
```

Resposta `503 Service Unavailable` para falhas temporarias de banco ou mensageria:

```json
{
  "erro": "ServicoIndisponivel",
  "mensagem": "Banco de dados ou mensageria temporariamente indisponivel",
  "detalhes": {
    "tentarNovamenteApos": 30
  }
}
```

### `GET /api/solicitacoes/eventos`

Lista eventos processados e persistidos.

Resposta `200 OK`:

```json
{
  "eventos": [
    {
      "id": 1,
      "nomeCliente": "Cliente Um",
      "nomeProduto": "Produto Um",
      "eventoId": "ES2-12345678-153015",
      "dataHoraEvento": "2026-05-10T15:30:15-03:00",
      "salvoEm": "2026-05-10T15:30:16-03:00"
    }
  ]
}
```

### `GET /api/solicitacoes/{id}/historico`

Retorna, em ordem de insercao, o historico imutavel de estados do pedido. Um processamento normal registra
`Recebido`, `Processando` e `Concluido`; falhas podem ser registradas como `Erro`.

Resposta `200 OK`:

```json
{
  "pedidoId": 1,
  "eventoId": "ES2-12345678-153015",
  "historico": [
    {
      "id": 1,
      "status": "Recebido",
      "registradoEm": "2026-05-10T15:30:15-03:00",
      "detalhe": null
    },
    {
      "id": 2,
      "status": "Processando",
      "registradoEm": "2026-05-10T15:30:16-03:00",
      "detalhe": null
    },
    {
      "id": 3,
      "status": "Concluido",
      "registradoEm": "2026-05-10T15:30:16-03:00",
      "detalhe": null
    }
  ]
}
```

IDs menores ou iguais a zero retornam `400 Bad Request`; pedidos inexistentes retornam `404 Not Found`.

## Configuracao

### Connection string

A API usa a primeira configuracao disponivel nesta ordem:

1. `ConnectionStrings:BancoPedidos`
2. `DATABASE_URL`
3. Valor padrao: `Host=localhost;Port=5432;Database=es2_pedidos;Username=dev;Password=dev`

A Lambda exige:

1. `ConnectionStrings:BancoPedidos`
2. `DATABASE_URL`

### AWS/SQS da API

A API resolve a regiao por:

- `AWS_REGIAO`
- `AWS_REGION`
- `AWS:Regiao`
- `AWS:Region`

A URL do servico AWS local/remoto pode ser configurada por:

- `AWS_ENDPOINT_URL`
- `AWS:ServiceUrl`
- `AWS:EndpointUrl`

A URL da fila pode ser configurada por:

- `SQS_FILA_URL`
- `SQS_QUEUE_URL`
- `AWS:FilaSolicitacoesUrl`
- `AWS:FilaPedidosUrl`
- `AWS:SqsQueueUrl`

No Docker Compose, a fila principal e:

```text
http://floci:4566/000000000000/processamento-solicitacoes
```

## Como Executar com Docker Compose

Este e o caminho recomendado para executar o fluxo completo API -> SQS -> Lambda -> PostgreSQL.

Pre-requisitos:

- Docker Desktop ou Docker Engine com Docker Compose.

Subir tudo a partir da raiz:

```powershell
docker compose -f docker/docker-compose.yml up --build
```

Servicos iniciados:

- `postgres`: banco PostgreSQL em `localhost:5432`.
- `database-init`: executa os scripts DDL/DML.
- `floci`: emulador AWS local em `localhost:4566`.
- `lambda-consumer-sqs-image`: build da imagem Lambda.
- `aws-init`: cria SQS, DLQ, Lambda e trigger SQS -> Lambda.
- `api`: API em `http://localhost:8080`.

URLs uteis:

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Health check: `http://localhost:8080/api/healthcheck`
- Emulador AWS: `http://localhost:4566`
- PostgreSQL: `localhost:5432`

Parar os containers:

```powershell
docker compose -f docker/docker-compose.yml down
```

Parar e remover volumes do banco:

```powershell
docker compose -f docker/docker-compose.yml down -v
```

## Como Executar Localmente sem Docker para a API

Para rodar a API diretamente com `dotnet run`, voce ainda precisa de PostgreSQL e SQS disponiveis.

Restaurar e compilar:

```powershell
dotnet restore ES2-SistemaPedidos.sln
dotnet build ES2-SistemaPedidos.sln
```

Rodar a API no perfil local:

```powershell
dotnet run --project src/ES2-SistemaPedidos.Api/ES2-SistemaPedidos.Api.csproj
```

O perfil local usa:

```text
http://localhost:5000
```

Se usar o Postgres e o Floci do Docker Compose, configure as variaveis locais da API para apontar para `localhost`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:AWS__Regiao = "us-east-1"
$env:AWS__ServiceUrl = "http://localhost:4566"
$env:SQS_FILA_URL = "http://localhost:4566/000000000000/processamento-solicitacoes"
$env:ConnectionStrings__BancoPedidos = "Host=localhost;Port=5432;Database=es2_pedidos;Username=dev;Password=dev"
dotnet run --project src/ES2-SistemaPedidos.Api/ES2-SistemaPedidos.Api.csproj
```

## Como Executar a Lambda Localmente

A Lambda normalmente e executada pelo Docker Compose atraves do Floci e do trigger SQS.

Para validar o projeto via console:

```powershell
dotnet run --project src/ES2-SistemaPedidos.LambdaConsumerSQS/ES2-SistemaPedidos.LambdaConsumerSQS.csproj
```

Para usar o AWS Lambda Test Tool:

```powershell
dotnet tool restore
dotnet-lambda-test-tool-8.0
```

O pacote de ferramentas esta declarado em `dotnet-tools.json`.

## Como Executar o Frontend

Opcoes:

1. Abrir diretamente `src/ES2-SistemaPedidos.FrontEnd/index.html` no navegador.
2. Servir a pasta com um servidor estatico.

Exemplo com Python:

```powershell
cd src/ES2-SistemaPedidos.FrontEnd
python -m http.server 8000
```

Depois acesse:

```text
http://localhost:8000
```

Com o Docker Compose, a API esperada pelo frontend ja e `http://localhost:8080/api`.

## Exemplos de Uso

Health check:

```powershell
Invoke-RestMethod http://localhost:8080/api/healthcheck
```

Criar solicitacao:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:8080/api/solicitacoes `
  -ContentType "application/json" `
  -Body '{"clienteId":1,"produtoId":1}'
```

Listar eventos:

```powershell
Invoke-RestMethod http://localhost:8080/api/solicitacoes/eventos
```

## Testes

### Todos os testes da solucao

```powershell
dotnet test ES2-SistemaPedidos.sln
```

### Testes unitarios da API

```powershell
dotnet test tests/unit/ES2-SistemaPedidos.Api.UnitTests/ES2-SistemaPedidos.Api.UnitTests.csproj
```

Cobrem:

- Validacao de `clienteId` e `produtoId`.
- Cliente inexistente.
- Produto inexistente.
- Publicacao de evento quando a solicitacao e valida.
- Listagem de eventos.
- Conversao de horario para Brasilia.

### Testes unitarios da Lambda

```powershell
dotnet test tests/unit/ES2-SistemaPedidos.LambdaConsumerSQS.UnitTests/ES2-SistemaPedidos.LambdaConsumerSQS.UnitTests.csproj
```

Cobrem:

- Processamento de payload valido.
- Rejeicao de payload invalido.
- Rejeicao de payload nulo.
- Registro do evento no repositorio.

### Testes unitarios do projeto compartilhado

```powershell
dotnet test tests/unit/ES2-SistemaPedidos.Shared.UnitTests/ES2-SistemaPedidos.Shared.UnitTests.csproj
```

Cobrem:

- Modelos de dominio.
- Formatter de logs com timestamp de Brasilia.

### Testes E2E

Os testes E2E esperam API, Postgres e fluxo SQS/Lambda disponiveis.

1. Suba a stack:

```powershell
docker compose -f docker/docker-compose.yml up --build
```

2. Em outro terminal, execute:

```powershell
dotnet test tests/e2e/ES2-SistemaPedidos.E2ETests/ES2-SistemaPedidos.E2ETests.csproj
```

Variaveis aceitas pelos E2E:

- `API_BASE_URL`, padrao `http://localhost:8080`.
- `DATABASE_URL`, padrao `Host=localhost;Port=5432;Database=es2_pedidos;Username=dev;Password=dev`.

Cenarios E2E:

- Criacao de nova solicitacao.
- Persistencia do evento no banco.
- Validacao dos dados salvos.
- Consulta de eventos pela API.
- Processamento de multiplas solicitacoes.
- Isolamento de dados entre testes.
- Cliente inexistente.
- Produto inexistente.
- Payload malformado.
- JSON vazio.
- Content-Type incorreto.
- Unicidade do ID de evento.
- Correcao de timestamps.

## Fluxo Interno

### Criacao de solicitacao

1. `PedidosController.CriarSolicitacaoAsync` recebe o POST.
2. `PedidoService.CriarSolicitacaoAsync` valida IDs maiores que zero.
3. O servico consulta `IClienteRepositorio` e `IProdutoRepositorio`.
4. O servico gera data/hora em Brasilia e `eventoId`.
5. `PedidoPublisherEventSqs` serializa `EventoSolicitacaoCliente`.
6. A mensagem e enviada para SQS com atributo `tipoEvento = SolicitacaoCliente`.
7. A API retorna `202 Accepted`.

### Processamento assincrono

1. O trigger SQS chama `Function.FunctionHandler`.
2. A funcao percorre cada mensagem do lote.
3. `ProcessadorPedidoService` desserializa e valida o payload.
4. `PedidoProcessamentoRepositoryDapper` insere o evento no banco.
5. Mensagens invalidas ou com erro sao retornadas em `BatchItemFailures`.

## Logs

A API usa Serilog configurado em `appsettings.json`.

O formatter `DateTimeConsoleFormatter` exibe logs com horario de Brasilia:

```text
[2026-05-03 12:04:05.123 -03:00 INF] Pedido 42 processado
```

## Troubleshooting

### API retorna `503 Service Unavailable`

Verifique:

- PostgreSQL esta rodando.
- A connection string esta correta.
- Floci esta rodando na porta `4566`.
- A fila SQS foi criada pelo `aws-init`.
- `SQS_FILA_URL` aponta para a fila correta.

### POST retorna `400 Bad Request`

Verifique:

- `clienteId` e `produtoId` devem ser maiores que zero.
- O cliente precisa existir na tabela `clientes`.
- O produto precisa existir na tabela `produtos`.

Para a carga inicial, use clientes/produtos `1` a `4`.

### Evento nao aparece em `GET /api/solicitacoes/eventos`

Verifique:

- A Lambda foi criada pelo `aws-init`.
- O trigger SQS -> Lambda existe.
- A mensagem nao foi enviada para a DLQ.
- A Lambda consegue conectar no banco.
- Aguarde alguns segundos, pois o processamento e assincrono.

### Porta ocupada

Portas usadas por padrao:

- API Docker: `8080`.
- API local: `5000`.
- PostgreSQL: `5432`.
- Floci/AWS local: `4566`.
- Frontend estatico sugerido: `8000`.

### Recriar ambiente do zero

```powershell
docker compose -f docker/docker-compose.yml down -v
docker compose -f docker/docker-compose.yml up --build
```

## Observacoes de Desenvolvimento

- A pasta `bin/` e `obj/` contem artefatos de build e nao fazem parte da estrutura logica do projeto.
- O projeto `tests/integration` existe como area reservada, mas nao possui testes implementados no estado atual.
- A API e a Lambda usam contratos compartilhados para evitar divergencia no payload do SQS.
- O banco evita duplicidade de eventos pelo indice unico em `evento_id`.
- O fluxo completo depende de mensageria; para testar criacao de solicitacoes com sucesso, use a stack Docker completa ou configure SQS equivalente.

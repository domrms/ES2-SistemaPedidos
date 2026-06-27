Feature: Fluxo de Pedidos

  Este arquivo descreve os cenários de teste de ponta a ponta para o fluxo de criação e consulta de pedidos.

@pedidos
Scenario: Criação de uma nova solicitação
  Given que o sistema está pronto e os dados de teste foram inicializados
  And que não há eventos de teste anteriores
  When uma solicitação POST é enviada para o endpoint de solicitações com cliente 9999 e produto 9999
  Then a resposta deve ser 202 Accepted
  And o corpo da resposta deve conter o clienteId, produtoId e um eventoId não vazio

@pedidos
Scenario: Persistência do evento no banco de dados
  Given que uma solicitação para o cliente 9999 e produto 9999 foi criada com sucesso
  When o sistema processa a mensagem da fila
  Then um registro de evento correspondente deve existir no banco de dados

@pedidos
Scenario: Validação dos dados do evento salvo
  Given que um evento para o cliente 9999 e produto 9999 foi salvo no banco
  When os dados desse evento são consultados
  Then os campos do evento devem corresponder aos dados de teste
  And o timestamp salvoEm deve ser válido

@pedidos
Scenario: Consulta de eventos pela API
  Given que o sistema pode ou não conter eventos
  When uma requisição GET é feita para o endpoint de eventos
  Then a resposta deve ser 200 OK
  And o corpo da resposta deve conter uma lista de eventos

@pedidos
Scenario: Processamento de múltiplas solicitações
  Given que o sistema está pronto
  When 3 solicitações para o cliente 9999 e produto 9999 são enviadas
  Then 3 eventos distintos devem ser salvos no banco de dados
Feature: Fluxo de Pedidos

  Cenários de ponta a ponta que protegem o contrato público e o efeito principal do fluxo de pedidos.

@pedidos
Scenario: Criação de solicitação aceita
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
Scenario: Consulta de eventos pela API
  Given que o sistema está pronto
  When uma requisição GET é feita para o endpoint de eventos
  Then a resposta deve ser 200 OK
  And o corpo da resposta deve conter uma lista de eventos

@pedidos
Scenario: Processamento de múltiplas solicitações
  Given que o sistema está pronto
  When 3 solicitações para o cliente 9999 e produto 9999 são enviadas
  Then 3 eventos distintos devem ser salvos no banco de dados

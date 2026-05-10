Feature: Validação e Tratamento de Erros

  Este arquivo descreve os cenários de teste de ponta a ponta para a validação de dados e tratamento de erros no sistema de pedidos.

@validacao
Scenario: Solicitação com cliente inexistente
  Given que o sistema está pronto
  When uma solicitação POST é enviada com o cliente 9998 e produto 9999
  Then a resposta deve ser 400 Bad Request

@validacao
Scenario: Solicitação com produto inexistente
  Given que o sistema está pronto
  When uma solicitação POST é enviada com o cliente 9999 e produto 9998
  Then a resposta deve ser 400 Bad Request

@validacao
Scenario: Solicitação com dados válidos
  Given que o sistema está pronto e os dados de teste existem
  When uma solicitação POST é enviada com o cliente 9999 e produto 9999
  Then a resposta deve ser 202 Accepted

@validacao
Scenario: Unicidade do ID do evento
  Given que o sistema está pronto
  When duas solicitações são feitas
  Then os eventoIds retornados devem ser diferentes

@validacao
Scenario: Consulta de eventos sem eventos de teste
  Given que a tabela de eventos de teste está limpa
  When uma requisição GET é feita para o endpoint de eventos
  Then a resposta deve ser 200 OK e não conter eventos de teste

@validacao
Scenario: Correção dos timestamps do evento
  Given que uma solicitação é criada
  When o evento correspondente é salvo no banco de dados
  Then o timestamp salvoEm deve ser maior ou igual ao dataHoraEvento

@validacao
Scenario: Solicitação com payload malformado
  Given que o sistema está pronto
  When uma solicitação POST é enviada com um payload malformado
  Then a resposta deve indicar um erro

@validacao
Scenario: Solicitação com JSON vazio
  Given que o sistema está pronto
  When uma solicitação POST é enviada com um JSON vazio
  Then a resposta deve indicar um erro

@validacao
Scenario: Solicitação com Content-Type incorreto
  Given que o sistema está pronto
  When uma solicitação POST é enviada com Content-Type incorreto
  Then a resposta deve ser 415 Unsupported Media Type

@validacao
Scenario: Filtragem de eventos
  Given que um evento para o cliente 9999 e produto 9999 é criado
  When a função de filtragem de eventos é chamada
  Then a lista retornada deve conter apenas eventos correspondentes

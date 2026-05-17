Feature: Validação e Tratamento de Erros

  Cenários de ponta a ponta para entradas inválidas no contrato de criação de pedidos.

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
Scenario: Solicitação com tipos inválidos
  Given que o sistema está pronto
  When uma solicitação POST é enviada com tipos inválidos
  Then a resposta deve ser 400 Bad Request

@validacao
Scenario: Solicitação com JSON vazio
  Given que o sistema está pronto
  When uma solicitação POST é enviada com um JSON vazio
  Then a resposta deve ser 400 Bad Request

@validacao
Scenario: Solicitação com Content-Type incorreto
  Given que o sistema está pronto
  When uma solicitação POST é enviada com Content-Type incorreto
  Then a resposta deve ser 415 Unsupported Media Type

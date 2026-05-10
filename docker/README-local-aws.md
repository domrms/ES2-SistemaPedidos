# Ambiente AWS local

Este compose sobe um ambiente local parecido com a AWS:

- `postgres`: banco da aplicacao.
- `floci`: emulador AWS para SQS, IAM e Lambda.
- `lambda-consumer-sqs-image`: build da imagem Lambda .NET 8 usada pelo LocalStack.
- `aws-init`: cria SQS, DLQ, Lambda e o trigger SQS -> Lambda.
- `api`: API .NET 10, publicando mensagens no SQS local.

## Subir tudo

```powershell
docker compose -f docker/docker-compose.yml up --build
```

API:

```text
http://localhost:8080/swagger
```

Floci:

```text
http://localhost:4566
```

## Fluxo esperado

1. A API grava/valida dados no Postgres.
2. A API publica a mensagem na fila `processamento-solicitacoes`.
3. O event source mapping do LocalStack entrega a mensagem para a Lambda.
4. A Lambda persiste o evento na tabela `eventos`.

## Comandos uteis

Listar filas:

```powershell
docker run --rm --network es2-sistema-pedidos -e AWS_ACCESS_KEY_ID=test -e AWS_SECRET_ACCESS_KEY=test -e AWS_DEFAULT_REGION=us-east-1 amazon/aws-cli --endpoint-url http://floci:4566 sqs list-queues
```

Ver logs do emulador AWS:

```powershell
docker compose -f docker/docker-compose.yml logs -f floci
```

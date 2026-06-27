#!/bin/sh
# Scripts montados em containers Linux devem permanecer com finais de linha LF.
set -eu

AWS_ENDPOINT_URL="${AWS_ENDPOINT_URL:-http://floci:4566}"
AWS_REGION="${AWS_DEFAULT_REGION:-us-east-1}"
QUEUE_NAME="${QUEUE_NAME:-processamento-solicitacoes}"
DLQ_NAME="${DLQ_NAME:-processamento-solicitacoes-dlq}"
FUNCTION_NAME="${FUNCTION_NAME:-processador-pedidos-sqs}"
ROLE_NAME="${ROLE_NAME:-lambda-sqs-role}"
LAMBDA_IMAGE="${LAMBDA_IMAGE:-es2-sistema-pedidos-lambda-consumer-sqs:latest}"
CONNECTION_STRING="${CONNECTION_STRING:-Host=postgres;Port=5432;Database=es2_pedidos;Username=dev;Password=dev}"

aws_local() {
  aws --endpoint-url "$AWS_ENDPOINT_URL" "$@"
}

echo "Aguardando emulador AWS em $AWS_ENDPOINT_URL..."
until aws_local sqs list-queues >/dev/null 2>&1; do
  sleep 2
done

echo "Criando filas SQS..."
DLQ_URL="$(aws_local sqs create-queue \
  --queue-name "$DLQ_NAME" \
  --query QueueUrl \
  --output text)"

DLQ_ARN="$(aws_local sqs get-queue-attributes \
  --queue-url "$DLQ_URL" \
  --attribute-names QueueArn \
  --query Attributes.QueueArn \
  --output text)"

QUEUE_URL="$(aws_local sqs create-queue \
  --queue-name "$QUEUE_NAME" \
  --query QueueUrl \
  --output text)"

QUEUE_ARN="$(aws_local sqs get-queue-attributes \
  --queue-url "$QUEUE_URL" \
  --attribute-names QueueArn \
  --query Attributes.QueueArn \
  --output text)"

cat > /tmp/queue-attributes.json <<EOF
{
  "VisibilityTimeout": "60",
  "RedrivePolicy": "{\"deadLetterTargetArn\":\"$DLQ_ARN\",\"maxReceiveCount\":5}"
}
EOF

aws_local sqs set-queue-attributes \
  --queue-url "$QUEUE_URL" \
  --attributes file:///tmp/queue-attributes.json

echo "Criando role IAM local..."
ASSUME_ROLE_POLICY='{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"Service":"lambda.amazonaws.com"},"Action":"sts:AssumeRole"}]}'
aws_local iam create-role \
  --role-name "$ROLE_NAME" \
  --assume-role-policy-document "$ASSUME_ROLE_POLICY" >/dev/null 2>&1 || true

ROLE_ARN="arn:aws:iam::000000000000:role/$ROLE_NAME"

cat > /tmp/lambda-environment.json <<EOF
{
  "Variables": {
    "ASPNETCORE_ENVIRONMENT": "Development",
    "DOTNET_ENVIRONMENT": "Development",
    "ConnectionStrings__BancoPedidos": "$CONNECTION_STRING"
  }
}
EOF

if aws_local lambda get-function --function-name "$FUNCTION_NAME" >/dev/null 2>&1; then
  echo "Atualizando Lambda $FUNCTION_NAME..."
  aws_local lambda update-function-code \
    --function-name "$FUNCTION_NAME" \
    --image-uri "$LAMBDA_IMAGE" >/dev/null
  aws_local lambda wait function-updated \
    --function-name "$FUNCTION_NAME" >/dev/null 2>&1 || true
  aws_local lambda update-function-configuration \
    --function-name "$FUNCTION_NAME" \
    --timeout 30 \
    --memory-size 256 \
    --environment file:///tmp/lambda-environment.json >/dev/null
else
  echo "Criando Lambda $FUNCTION_NAME..."
  aws_local lambda create-function \
    --function-name "$FUNCTION_NAME" \
    --package-type Image \
    --code ImageUri="$LAMBDA_IMAGE" \
    --role "$ROLE_ARN" \
    --timeout 30 \
    --memory-size 256 \
    --environment file:///tmp/lambda-environment.json >/dev/null
fi

aws_local lambda wait function-active \
  --function-name "$FUNCTION_NAME" >/dev/null 2>&1 || true

MAPPING_UUID="$(aws_local lambda list-event-source-mappings \
  --function-name "$FUNCTION_NAME" \
  --event-source-arn "$QUEUE_ARN" \
  --query 'EventSourceMappings[0].UUID' \
  --output text)"

if [ -z "$MAPPING_UUID" ] || [ "$MAPPING_UUID" = "None" ]; then
  echo "Criando trigger SQS -> Lambda..."
  aws_local lambda create-event-source-mapping \
    --function-name "$FUNCTION_NAME" \
    --event-source-arn "$QUEUE_ARN" \
    --batch-size 10 \
    --function-response-types ReportBatchItemFailures >/dev/null
else
  echo "Trigger ja existe: $MAPPING_UUID"
  aws_local lambda update-event-source-mapping \
    --uuid "$MAPPING_UUID" \
    --enabled \
    --batch-size 10 \
    --function-response-types ReportBatchItemFailures >/dev/null
fi

echo "Infraestrutura AWS local provisionada."
echo "SQS Queue URL: $QUEUE_URL"
echo "SQS DLQ URL: $DLQ_URL"
echo "Lambda: $FUNCTION_NAME"

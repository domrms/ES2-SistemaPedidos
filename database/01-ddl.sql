CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS pedidos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id VARCHAR(255) NOT NULL,
    valor_total NUMERIC(19, 2) NOT NULL,
    status SMALLINT NOT NULL DEFAULT 0,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    atualizado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    processamento_iniciado_em TIMESTAMPTZ NULL,
    concluido_em TIMESTAMPTZ NULL,
    mensagem_erro TEXT NULL,
    motivo_aprovacao TEXT NULL,
    motivo_rejeicao TEXT NULL,
    CONSTRAINT ck_pedidos_valor_total_positivo CHECK (valor_total > 0),
    CONSTRAINT ck_pedidos_status_valido CHECK (status IN (0, 1, 2, 3, 4)),
    CONSTRAINT ck_pedidos_concluido_apenas_terminal
        CHECK (concluido_em IS NULL OR status IN (2, 3, 4))
);

CREATE INDEX IF NOT EXISTS ix_pedidos_cliente_id ON pedidos (cliente_id);
CREATE INDEX IF NOT EXISTS ix_pedidos_status ON pedidos (status);
CREATE INDEX IF NOT EXISTS ix_pedidos_criado_em ON pedidos (criado_em);
CREATE INDEX IF NOT EXISTS ix_pedidos_status_atualizado_em ON pedidos (status, atualizado_em);

CREATE TABLE IF NOT EXISTS itens_pedido (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pedido_id UUID NOT NULL,
    produto_id VARCHAR(255) NOT NULL,
    quantidade INTEGER NOT NULL,
    preco_unitario NUMERIC(19, 2) NOT NULL,
    valor_linha NUMERIC(19, 2) NOT NULL,
    descricao VARCHAR(500) NULL,
    CONSTRAINT fk_itens_pedido_pedidos
        FOREIGN KEY (pedido_id) REFERENCES pedidos (id) ON DELETE CASCADE,
    CONSTRAINT ck_itens_pedido_quantidade_positiva CHECK (quantidade > 0),
    CONSTRAINT ck_itens_pedido_preco_unitario_nao_negativo CHECK (preco_unitario >= 0),
    CONSTRAINT ck_itens_pedido_valor_linha_nao_negativo CHECK (valor_linha >= 0),
    CONSTRAINT ck_itens_pedido_valor_linha_calculado
        CHECK (abs(valor_linha - (quantidade * preco_unitario)) <= 0.01)
);

CREATE INDEX IF NOT EXISTS ix_itens_pedido_pedido_id ON itens_pedido (pedido_id);
CREATE INDEX IF NOT EXISTS ix_itens_pedido_produto_id ON itens_pedido (produto_id);

CREATE TABLE IF NOT EXISTS mensagens_processadas (
    mensagem_id VARCHAR(255) PRIMARY KEY,
    pedido_id UUID NULL,
    processada_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    tipo_mensagem VARCHAR(100) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'SUCESSO',
    detalhes_erro TEXT NULL,
    CONSTRAINT fk_mensagens_processadas_pedidos
        FOREIGN KEY (pedido_id) REFERENCES pedidos (id) ON DELETE SET NULL,
    CONSTRAINT ck_mensagens_processadas_status
        CHECK (status IN ('SUCESSO', 'FALHA', 'DLQ'))
);

CREATE INDEX IF NOT EXISTS ix_mensagens_processadas_pedido_processada_em
    ON mensagens_processadas (pedido_id, processada_em);

CREATE INDEX IF NOT EXISTS ix_mensagens_processadas_tipo_processada_em
    ON mensagens_processadas (tipo_mensagem, processada_em);

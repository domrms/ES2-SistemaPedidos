CREATE TABLE IF NOT EXISTS clientes (
    id INTEGER PRIMARY KEY,
    nome VARCHAR(255) NOT NULL
);

CREATE TABLE IF NOT EXISTS produtos (
    id INTEGER PRIMARY KEY,
    nome VARCHAR(255) NOT NULL
);

CREATE TABLE IF NOT EXISTS eventos (
    id BIGSERIAL PRIMARY KEY,
    cliente_id INTEGER NOT NULL,
    produto_id INTEGER NOT NULL,
    evento_id VARCHAR(20) NOT NULL,
    data_hora_evento TIMESTAMPTZ NOT NULL,
    salvo_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT fk_eventos_clientes
        FOREIGN KEY (cliente_id) REFERENCES clientes (id) ON DELETE RESTRICT,
    CONSTRAINT fk_eventos_produtos
        FOREIGN KEY (produto_id) REFERENCES produtos (id) ON DELETE RESTRICT,
    CONSTRAINT uq_eventos_evento_id UNIQUE (evento_id)
);

CREATE INDEX IF NOT EXISTS ix_eventos_cliente_produto_data_hora
    ON eventos (cliente_id, produto_id, data_hora_evento);

CREATE TABLE IF NOT EXISTS pedido_status (
    id BIGSERIAL PRIMARY KEY,
    pedido_id BIGINT NOT NULL,
    status VARCHAR(20) NOT NULL,
    registrado_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    detalhe VARCHAR(500),
    CONSTRAINT fk_pedido_status_eventos
        FOREIGN KEY (pedido_id) REFERENCES eventos (id) ON DELETE CASCADE,
    CONSTRAINT uq_pedido_status_pedido_status UNIQUE (pedido_id, status),
    CONSTRAINT ck_pedido_status_status
        CHECK (status IN ('Recebido', 'Processando', 'Concluido', 'Erro'))
);

CREATE INDEX IF NOT EXISTS ix_pedido_status_pedido_id_id
    ON pedido_status (pedido_id, id);

CREATE OR REPLACE FUNCTION impedir_alteracao_pedido_status()
RETURNS TRIGGER AS $$
BEGIN
    -- Exclusoes disparadas pelo ON DELETE CASCADE da raiz continuam permitidas.
    IF TG_OP = 'UPDATE' OR pg_trigger_depth() = 1 THEN
        RAISE EXCEPTION 'O historico de status do pedido e imutavel';
    END IF;
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_pedido_status_append_only ON pedido_status;
CREATE TRIGGER trg_pedido_status_append_only
BEFORE UPDATE OR DELETE ON pedido_status
FOR EACH ROW EXECUTE FUNCTION impedir_alteracao_pedido_status();

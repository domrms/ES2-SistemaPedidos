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

INSERT INTO clientes (id, nome)
VALUES
    (1, 'Cliente Um'),
    (2, 'Cliente Dois'),
    (3, 'Cliente Tres'),
    (4, 'Cliente Quatro')
ON CONFLICT (id) DO UPDATE
SET nome = EXCLUDED.nome;

INSERT INTO produtos (id, nome)
VALUES
    (1, 'Produto Um'),
    (2, 'Produto Dois'),
    (3, 'Produto Tres'),
    (4, 'Produto Quatro')
ON CONFLICT (id) DO UPDATE
SET nome = EXCLUDED.nome;

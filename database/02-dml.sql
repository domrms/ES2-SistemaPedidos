INSERT INTO clientes (id, nome)
VALUES
    (1, 'Cliente Um'),
    (2, 'Cliente Dois'),
    (3, 'Cliente Tres'),
    (4, 'Cliente Quatro')
ON CONFLICT (id) DO UPDATE
SET nome = EXCLUDED.nome;

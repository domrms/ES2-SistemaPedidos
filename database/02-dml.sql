INSERT INTO pedidos (
    id,
    cliente_id,
    valor_total,
    status,
    criado_em,
    atualizado_em,
    processamento_iniciado_em,
    concluido_em,
    motivo_aprovacao,
    motivo_rejeicao,
    mensagem_erro
)
VALUES
    (
        '11111111-1111-1111-1111-111111111111',
        'CLIENTE-001',
        750.00,
        0,
        now() - interval '15 minutes',
        now() - interval '15 minutes',
        NULL,
        NULL,
        NULL,
        NULL,
        NULL
    ),
    (
        '22222222-2222-2222-2222-222222222222',
        'CLIENTE-002',
        250.00,
        2,
        now() - interval '30 minutes',
        now() - interval '28 minutes',
        now() - interval '29 minutes',
        now() - interval '28 minutes',
        'Valor abaixo do limite de aprovacao',
        NULL,
        NULL
    ),
    (
        '33333333-3333-3333-3333-333333333333',
        'CLIENTE-003',
        2500.00,
        3,
        now() - interval '45 minutes',
        now() - interval '43 minutes',
        now() - interval '44 minutes',
        now() - interval '43 minutes',
        NULL,
        'Valor igual ou acima do limite de aprovacao',
        NULL
    ),
    (
        '44444444-4444-4444-4444-444444444444',
        'CLIENTE-004',
        125.00,
        4,
        now() - interval '60 minutes',
        now() - interval '58 minutes',
        now() - interval '59 minutes',
        now() - interval '58 minutes',
        NULL,
        NULL,
        'Falha simulada no processamento'
    )
ON CONFLICT (id) DO NOTHING;

INSERT INTO itens_pedido (
    id,
    pedido_id,
    produto_id,
    quantidade,
    preco_unitario,
    valor_linha,
    descricao
)
VALUES
    (
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1',
        '11111111-1111-1111-1111-111111111111',
        'PROD-A',
        2,
        300.00,
        600.00,
        'Item premium'
    ),
    (
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2',
        '11111111-1111-1111-1111-111111111111',
        'PROD-B',
        1,
        150.00,
        150.00,
        'Item padrao'
    ),
    (
        'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1',
        '22222222-2222-2222-2222-222222222222',
        'PROD-C',
        5,
        50.00,
        250.00,
        'Pedido aprovado de exemplo'
    ),
    (
        'cccccccc-cccc-cccc-cccc-ccccccccccc1',
        '33333333-3333-3333-3333-333333333333',
        'PROD-D',
        1,
        2500.00,
        2500.00,
        'Pedido rejeitado de exemplo'
    ),
    (
        'dddddddd-dddd-dddd-dddd-ddddddddddd1',
        '44444444-4444-4444-4444-444444444444',
        'PROD-E',
        1,
        125.00,
        125.00,
        'Pedido com falha de exemplo'
    )
ON CONFLICT (id) DO NOTHING;

INSERT INTO mensagens_processadas (
    mensagem_id,
    pedido_id,
    processada_em,
    tipo_mensagem,
    status,
    detalhes_erro
)
VALUES
    (
        'sns-msg-aprovado-001',
        '22222222-2222-2222-2222-222222222222',
        now() - interval '28 minutes',
        'PedidoCriado',
        'SUCESSO',
        NULL
    ),
    (
        'sns-msg-rejeitado-001',
        '33333333-3333-3333-3333-333333333333',
        now() - interval '43 minutes',
        'PedidoCriado',
        'SUCESSO',
        NULL
    ),
    (
        'sns-msg-falhou-001',
        '44444444-4444-4444-4444-444444444444',
        now() - interval '58 minutes',
        'PedidoCriado',
        'FALHA',
        'Falha simulada no processamento'
    )
ON CONFLICT (mensagem_id) DO NOTHING;

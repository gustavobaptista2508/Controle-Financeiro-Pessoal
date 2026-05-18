CREATE TABLE investimentos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL,
    nome VARCHAR(120) NOT NULL,
    ticker VARCHAR(20) NULL,
    tipo VARCHAR(50) NOT NULL,
    valor_investido DECIMAL(18,2) NOT NULL,
    quantidade DECIMAL(18,6) NOT NULL,
    valor_atual_unitario DECIMAL(18,2) NOT NULL,
    data_compra DATETIME NOT NULL,
    observacao VARCHAR(500) NULL,
    data_criacao DATETIME NOT NULL,
    data_atualizacao DATETIME NULL,
    INDEX idx_investimentos_usuario (usuario_id)
);

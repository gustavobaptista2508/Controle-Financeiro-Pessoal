CREATE TABLE IF NOT EXISTS objetivos_financeiros (
    id INT NOT NULL AUTO_INCREMENT,
    usuario_id INT NOT NULL,
    nome VARCHAR(120) NOT NULL,
    valor_alvo DECIMAL(18,2) NOT NULL DEFAULT 0,
    valor_atual DECIMAL(18,2) NOT NULL DEFAULT 0,
    data_meta DATETIME NULL,
    cor VARCHAR(20) NULL,
    icone VARCHAR(80) NULL,
    ativo TINYINT(1) NOT NULL DEFAULT 1,
    data_criacao DATETIME NOT NULL,
    data_atualizacao DATETIME NULL,
    PRIMARY KEY (id),
    INDEX ix_objetivos_financeiros_usuario_id (usuario_id),
    CONSTRAINT fk_objetivos_financeiros_usuarios_usuario_id
        FOREIGN KEY (usuario_id) REFERENCES usuarios (id)
        ON DELETE RESTRICT
);

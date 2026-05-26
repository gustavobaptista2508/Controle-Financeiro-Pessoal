-- Executar no MySQL para alinhar a tabela usuarios com o model Usuario
-- Cada comando é idempotente em ambientes onde IF NOT EXISTS é suportado (MySQL 8+).

ALTER TABLE usuarios
    ADD COLUMN IF NOT EXISTS trial_lembrete_enviado_em DATETIME NULL;

ALTER TABLE usuarios
    ADD COLUMN IF NOT EXISTS trial_encerrado_email_enviado_em DATETIME NULL;

ALTER TABLE usuarios
    ADD COLUMN IF NOT EXISTS trial_aviso_7_dias_enviado_em DATETIME NULL;

ALTER TABLE usuarios
    ADD COLUMN IF NOT EXISTS trial_aviso_3_dias_enviado_em DATETIME NULL;

ALTER TABLE usuarios
    ADD COLUMN IF NOT EXISTS trial_aviso_1_dia_enviado_em DATETIME NULL;

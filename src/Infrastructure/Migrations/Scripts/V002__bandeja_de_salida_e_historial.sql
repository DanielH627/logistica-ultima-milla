-- V002: bandeja de salida e historial de eventos (esquema transversal).
-- La bandeja de salida guarda los eventos junto con la entrega, en la misma transacción,
-- y un procesador los entrega después. Así un evento nunca se pierde si algo falla.

CREATE SCHEMA IF NOT EXISTS transversal;

CREATE TABLE transversal.bandeja_salida (
    id               uuid        PRIMARY KEY,
    aggregate_id     uuid        NOT NULL,
    message_type     text        NOT NULL,
    payload          jsonb       NOT NULL,
    occurred_at      timestamptz NOT NULL,
    status           text        NOT NULL DEFAULT 'Pending',
    attempts         integer     NOT NULL DEFAULT 0,
    next_attempt_at  timestamptz NOT NULL DEFAULT now(),
    last_error       text        NULL,
    processed_at     timestamptz NULL,
    CONSTRAINT ck_bandeja_estado CHECK (status IN ('Pending', 'Sent', 'Dead')),
    CONSTRAINT ck_bandeja_intentos CHECK (attempts >= 0)
);

-- Los mensajes pendientes se consultan por su próximo intento.
CREATE INDEX ix_bandeja_pendientes ON transversal.bandeja_salida (next_attempt_at)
    WHERE status = 'Pending';

CREATE INDEX ix_bandeja_agregado ON transversal.bandeja_salida (aggregate_id);

CREATE TABLE transversal.historial_eventos (
    id            uuid        PRIMARY KEY,
    -- Un mensaje se registra una sola vez, aunque se procese más de una vez.
    message_id    uuid        NOT NULL UNIQUE,
    aggregate_id  uuid        NOT NULL,
    event_type    text        NOT NULL,
    payload       jsonb       NOT NULL,
    occurred_at   timestamptz NOT NULL,
    recorded_at   timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX ix_historial_agregado ON transversal.historial_eventos (aggregate_id, occurred_at);

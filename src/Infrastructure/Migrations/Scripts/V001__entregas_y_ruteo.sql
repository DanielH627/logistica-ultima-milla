-- V001: esquemas y tablas de los módulos Entregas y Ruteo.
-- Cada módulo tiene su propio esquema, lo que mantiene separados sus datos (ADR-001).

CREATE SCHEMA IF NOT EXISTS entregas;
CREATE SCHEMA IF NOT EXISTS ruteo;

CREATE TABLE entregas.entrega (
    id            uuid        PRIMARY KEY,
    order_id      uuid        NOT NULL,
    address       text        NOT NULL,
    window_start  timestamptz NOT NULL,
    window_end    timestamptz NOT NULL,
    priority      text        NOT NULL,
    status        text        NOT NULL,
    courier_id    uuid        NULL,
    assigned_at   timestamptz NULL,
    version       integer     NOT NULL,
    CONSTRAINT ck_entrega_ventana CHECK (window_start < window_end),
    CONSTRAINT ck_entrega_prioridad CHECK (priority IN ('Normal', 'Urgent')),
    CONSTRAINT ck_entrega_estado CHECK (status IN
        ('PendingAssignment', 'Assigned', 'InTransit', 'Delivered', 'Failed', 'Cancelled')),
    -- Defensa adicional de INV03. La regla la protege el dominio; la base solo la respalda.
    CONSTRAINT ck_entrega_repartidor_coherente CHECK (
        (status = 'PendingAssignment' AND courier_id IS NULL)
        OR (status IN ('Assigned', 'InTransit') AND courier_id IS NOT NULL)
        OR status IN ('Delivered', 'Failed', 'Cancelled'))
);

-- Una entrega por pedido (decisión provisional del documento 02 de la Actividad 3).
CREATE UNIQUE INDEX ux_entrega_pedido ON entregas.entrega (order_id);

CREATE TABLE ruteo.repartidor (
    id         uuid    PRIMARY KEY,
    name       text    NOT NULL,
    is_active  boolean NOT NULL
);

CREATE TABLE ruteo.ruta (
    id          uuid    PRIMARY KEY,
    courier_id  uuid    NOT NULL REFERENCES ruteo.repartidor (id),
    route_date  date    NOT NULL,
    max_stops   integer NOT NULL,
    CONSTRAINT ck_ruta_capacidad CHECK (max_stops >= 1),
    CONSTRAINT ux_ruta_repartidor_fecha UNIQUE (courier_id, route_date)
);

CREATE TABLE ruteo.parada (
    route_id       uuid        NOT NULL REFERENCES ruteo.ruta (id),
    delivery_id    uuid        NOT NULL,
    stop_position  integer     NOT NULL,
    reserved_at    timestamptz NOT NULL DEFAULT now(),
    expires_at     timestamptz NULL,
    PRIMARY KEY (route_id, delivery_id),
    CONSTRAINT ck_parada_posicion CHECK (stop_position >= 1)
);

CREATE INDEX ix_parada_entrega ON ruteo.parada (delivery_id);

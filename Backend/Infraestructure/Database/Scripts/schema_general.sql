-- =====================================================================
-- Schema: General
-- Sistema de monitore LoRaWAN (LoRaMonitor)
-- =====================================================================

CREATE SCHEMA IF NOT EXISTS "general";

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS postgis;

-- =====================================================================
-- Function and trigger for the 'edited_at' field.
-- =====================================================================

CREATE OR REPLACE FUNCTION "general".set_edited_at()
RETURNS trigger AS $$
BEGIN
    NEW.edited_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- =====================================================================
-- Table: organisations
-- =====================================================================

CREATE TABLE "general".organisations (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(100) NOT NULL,
    cuit        VARCHAR(20) NULL,
    active      BOOLEAN DEFAULT true NOT NULL,
    created_at  TIMESTAMPTZ DEFAULT now() NOT NULL,
    edited_at   TIMESTAMPTZ DEFAULT now() NOT NULL
);

CREATE TRIGGER trg_organisations_edited_at
    BEFORE UPDATE ON "general".organisations
    FOR EACH ROW EXECUTE FUNCTION "general".set_edited_at();

-- =====================================================================
-- Table: users
-- =====================================================================

CREATE TYPE "general".user_role AS ENUM (
    'SUPERADMIN',
    'ORG_ADMIN',
    'OPERATOR'
);

CREATE TABLE "general".users (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    org_id              UUID NULL,
    fullname            VARCHAR(150) NOT NULL,
    email               VARCHAR(150) NOT NULL UNIQUE,
    email_verified_at   TIMESTAMPTZ NULL,
    pending_mail        VARCHAR(150),
    password_hash       VARCHAR(255) NOT NULL,
    role                "general".user_role NOT NULL,
    active              BOOLEAN DEFAULT true NOT NULL,
    created_at          TIMESTAMPTZ DEFAULT now() NOT NULL,
    edited_at           TIMESTAMPTZ DEFAULT now() NOT NULL,

    CONSTRAINT fk_user_organisation
        FOREIGN KEY (org_id)
        REFERENCES "general".organisations(id)
        ON DELETE CASCADE,

    CONSTRAINT chk_superadmin_organisation CHECK(
        (role = 'SUPERADMIN' AND org_id IS NULL)
        OR
        (role <> 'SUPERADMIN' AND org_id IS NOT NULL)
    )
);

CREATE INDEX idx_users_org_id ON "general".users (org_id);

CREATE TRIGGER trg_users_edited_at
    BEFORE UPDATE ON "general".users
    FOR EACH ROW EXECUTE FUNCTION "general".set_edited_at();

-- =====================================================================
-- Table: user_tokens
-- =====================================================================

CREATE TYPE "general".token_type AS ENUM (
    'EMAIL_VERIFICATION',
    'PASSWORD_RESET',
    'EMAIL_CHANGE'
);

CREATE TABLE "general".user_tokens (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID NOT NULL,
    type        "general".token_type NOT NULL,
    token_hash  VARCHAR(255) NOT NULL,
    expires_at  TIMESTAMPTZ NOT NULL,
    used_at     TIMESTAMPTZ NULL,
    created_at  TIMESTAMPTZ DEFAULT now() NOT NULL,

    CONSTRAINT fk_user_tokens_user
        FOREIGN KEY (user_id)
        REFERENCES "general".users(id)
        ON DELETE CASCADE
);

CREATE INDEX idx_user_tokens_user_id ON "general".user_tokens (user_id);
CREATE UNIQUE INDEX idx_user_tokens_hash ON "general".user_tokens (token_hash);
CREATE INDEX idx_user_tokens_active ON "general".user_tokens (user_id, type) WHERE used_at IS NULL;

-- =====================================================================
-- Table: user_sessions
-- =====================================================================

CREATE TABLE "general".user_sessions (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id             UUID NOT NULL,
    refresh_token_hash  VARCHAR(255) NOT NULL UNIQUE,
    ip_address          INET NULL,               -- Tipo de dato nativo de Postgres para IPv4 e IPv6
    user_agent          TEXT NULL,               -- Ej: "Chrome en Windows 11"
    expires_at          TIMESTAMPTZ NOT NULL,
    revoked_at          TIMESTAMPTZ NULL,        -- Si no es NULL, la sesión fue cerrada manualmente
    created_at          TIMESTAMPTZ DEFAULT now() NOT NULL,

    CONSTRAINT fk_user_sessions_user
        FOREIGN KEY (user_id)
        REFERENCES "general".users(id)
        ON DELETE CASCADE
);

CREATE INDEX idx_user_sessions_user_id ON "general".user_sessions (user_id);
CREATE INDEX idx_user_sessions_active ON "general".user_sessions (user_id) WHERE revoked_at IS NULL;

-- =====================================================================
-- Table: nodes
-- =====================================================================

CREATE TYPE "general".node_type AS ENUM (
    'MODBUS_RTU',
    'PULSE',
    'ANALOG'
);

CREATE TYPE "general".node_state AS ENUM (
    'ACTIVE',
    'INACTIVE',
    'MAINTENANCE'
);

CREATE TABLE "general".nodes (
    dev_eui             VARCHAR(16) PRIMARY KEY NOT NULL,
    org_id              UUID NOT NULL,
    alias               VARCHAR(100) NOT NULL,
    meter_type          "general".node_type NULL,
    config              JSONB NULL,
    freq_minutes        INT NULL,
    operative_state     "general".node_state DEFAULT 'INACTIVE' NOT NULL,
    coordinates         geometry(Point, 4326) NULL,
    created_at          TIMESTAMPTZ DEFAULT now() NOT NULL,
    edited_at           TIMESTAMPTZ DEFAULT now() NOT NULL,

    CONSTRAINT fk_nodes_organisation
        FOREIGN KEY (org_id)
        REFERENCES "general".organisations(id)
        ON DELETE CASCADE
);

CREATE INDEX idx_nodes_org_id ON "general".nodes (org_id);
CREATE INDEX idx_nodes_coordinates ON "general".nodes USING GIST (coordinates);

CREATE TRIGGER trg_nodes_edited_at
    BEFORE UPDATE ON "general".nodes
    FOR EACH ROW EXECUTE FUNCTION "general".set_edited_at();

-- =====================================================================
-- Table: gateways
-- =====================================================================

CREATE TYPE "general".gateway_state AS ENUM (
    'ACTIVE',
    'INACTIVE',
    'MAINTENANCE'
);

CREATE TABLE "general".gateways (
    gateway_eui         VARCHAR(16) PRIMARY KEY NOT NULL,
    org_id              UUID NULL,
    alias               VARCHAR(100) NOT NULL,
    model               VARCHAR(100) NOT NULL,
    coordinates         geometry(Point, 4326) NULL,
    operative_state     "general".gateway_state DEFAULT 'INACTIVE' NOT NULL,
    created_at          TIMESTAMPTZ DEFAULT now() NOT NULL,
    edited_at           TIMESTAMPTZ DEFAULT now() NOT NULL,
    last_seen           TIMESTAMPTZ NULL,

    CONSTRAINT fk_gateway_organisation
        FOREIGN KEY (org_id)
        REFERENCES "general".organisations(id)
        ON DELETE SET NULL
);

CREATE INDEX idx_gateways_org_id ON "general".gateways (org_id);
CREATE INDEX idx_gateways_coordinates ON "general".gateways USING GIST (coordinates);

CREATE TRIGGER trg_gateways_edited_at
    BEFORE UPDATE ON "general".gateways
    FOR EACH ROW EXECUTE FUNCTION "general".set_edited_at();

-- =====================================================================
-- Table: alarms
-- =====================================================================

CREATE TYPE "general".alarm_type AS ENUM (
    'VOLTAGE_MAX',
    'VOLTAGE_MIN',
    'CURRENT_MAX',
    'CURRENT_MIN',
    'POWER_MAX',
    'POWER_MIN',
    'POWER_OUTAGE'
);

CREATE TYPE "general".alarm_severity AS ENUM (
    'INFO',
    'WARNING',
    'CRITICAL'
);

CREATE TABLE "general".alarms (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    dev_eui         VARCHAR(16) NOT NULL,
    alarm           "general".alarm_type NOT NULL,
    threshold_min   NUMERIC(10, 2) NULL,
    threshold_max   NUMERIC(10, 2) NULL,
    severity        "general".alarm_severity NOT NULL,
    active          BOOLEAN DEFAULT true NOT NULL,
    created_at      TIMESTAMPTZ DEFAULT now() NOT NULL,
    edited_at       TIMESTAMPTZ DEFAULT now() NOT NULL,

    CONSTRAINT fk_alarms_nodes
        FOREIGN KEY (dev_eui)
        REFERENCES "general".nodes(dev_eui)
        ON DELETE CASCADE,

    CONSTRAINT chk_alarm_thresholds CHECK (
        alarm = 'POWER_OUTAGE'
        OR threshold_min IS NOT NULL
        OR threshold_max IS NOT NULL
    )
);

CREATE INDEX  idx_alarms_dev_eui ON "general".alarms (dev_eui);

CREATE TRIGGER trg_alarms_edited_at
    BEFORE UPDATE ON "general".alarms
    FOR EACH ROW EXECUTE FUNCTION "general".set_edited_at();

-- =====================================================================
-- Table: notifications
-- =====================================================================

CREATE TYPE "general".notification_method AS ENUM (
    'MAIL',
    'TELEGRAM'
);

CREATE TABLE "general".notifications (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    org_id      UUID NOT NULL,
    method      "general".notification_method NOT NULL,
    config      JSONB NOT NULL,
    active      BOOLEAN DEFAULT true NOT NULL,
    created_at  TIMESTAMPTZ DEFAULT now() NOT NULL,
    edited_at   TIMESTAMPTZ DEFAULT now() NOT NULL,

    CONSTRAINT fk_notification_organisation
        FOREIGN KEY (org_id)
        REFERENCES "general".organisations(id)
        ON DELETE CASCADE
);

CREATE INDEX idx_notifications_org_id ON "general".notifications (org_id);

CREATE TRIGGER trg_notifications_edited_at
    BEFORE UPDATE ON "general".notifications
    FOR EACH ROW EXECUTE FUNCTION "general".set_edited_at();

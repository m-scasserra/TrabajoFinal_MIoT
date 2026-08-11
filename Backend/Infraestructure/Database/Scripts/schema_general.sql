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
-- Custom ENUM types
-- =====================================================================

CREATE TYPE "general".user_role AS ENUM (
    'SUPERADMIN',
    'ORG_ADMIN',
    'OPERATOR'
);

CREATE TYPE "general".token_type AS ENUM (
    'EMAIL_VERIFICATION',
    'PASSWORD_RESET',
    'EMAIL_CHANGE'
);

CREATE TYPE "general".gateway_state AS ENUM (
    'ACTIVE',
    'INACTIVE',
    'MAINTENANCE'
);

CREATE TYPE "general".sync_status AS ENUM (
    'PENDING',
    'SYNCED',
    'FAILED',
    'PENDING_DELETE',
    'DELETE_FAILED'
);

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

CREATE TYPE "general".notification_method AS ENUM (
    'MAIL',
    'TELEGRAM'
);

CREATE TYPE "general".lorawan_mac_version AS ENUM (
    'LORAWAN_1_0_0',
    'LORAWAN_1_0_1',
    'LORAWAN_1_0_2',
    'LORAWAN_1_0_3',
    'LORAWAN_1_0_4',
    'LORAWAN_1_1_0'
);

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
-- Table: gateways
-- =====================================================================

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
    sync_status         "general".sync_status DEFAULT 'PENDING' NOT NULL,
    synced_at           TIMESTAMPTZ NULL,
    sync_error          TEXT NULL,

    CONSTRAINT fk_gateway_organisation
        FOREIGN KEY (org_id)
        REFERENCES "general".organisations(id)
        ON DELETE SET NULL
);

CREATE INDEX idx_gateways_org_id ON "general".gateways (org_id);
CREATE INDEX idx_gateways_coordinates ON "general".gateways USING GIST (coordinates);
CREATE INDEX idx_gateways_sync_status ON "general".gateways (sync_status) WHERE sync_status <> 'SYNCED';

CREATE TRIGGER trg_gateways_edited_at
    BEFORE UPDATE ON "general".gateways
    FOR EACH ROW EXECUTE FUNCTION "general".set_edited_at();

-- =====================================================================
-- Table: nodes
-- =====================================================================

CREATE TABLE "general".nodes (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    mac_address         VARCHAR(12) NOT NULL,
    dev_eui             VARCHAR(16) NULL,
    hw_revision         SMALLINT NOT NULL,
    fw_revision         SMALLINT NOT NULL,
    app_key_encrypted   BYTEA NOT NULL,
    device_profile_id   UUID NULL,
    org_id              UUID NULL,
    alias               VARCHAR(100) NOT NULL,
    meter_type          "general".node_type NULL,
    config              JSONB NULL,
    freq_minutes        INT NULL,
    operative_state     "general".node_state DEFAULT 'INACTIVE' NOT NULL,
    coordinates         geometry(Point, 4326) NULL,
    created_at          TIMESTAMPTZ DEFAULT now() NOT NULL,
    edited_at           TIMESTAMPTZ DEFAULT now() NOT NULL,
    sync_status         "general".sync_status DEFAULT 'PENDING' NOT NULL,
    synced_at           TIMESTAMPTZ NULL,
    sync_error          TEXT NULL,

    CONSTRAINT fk_nodes_organisation
        FOREIGN KEY (org_id)
        REFERENCES "general".organisations(id)
        ON DELETE CASCADE,
    
    CONSTRAINT fk_nodes_device_profile
        FOREIGN KEY (device_profile_id)
        REFERENCES "general".device_profiles(id)
        ON DELETE RESTRICT
);

CREATE INDEX idx_nodes_org_id ON "general".nodes (org_id);
CREATE INDEX idx_nodes_coordinates ON "general".nodes USING GIST (coordinates);
CREATE UNIQUE INDEX idx_nodes_mac ON "general".nodes (mac_address);
CREATE INDEX idx_nodes_available ON "general".nodes (org_id) WHERE org_id IS NULL;
CREATE INDEX idx_nodes_sync_status ON "general".nodes (sync_status) WHERE sync_status <> 'SYNCED';

CREATE TRIGGER trg_nodes_edited_at
    BEFORE UPDATE ON "general".nodes
    FOR EACH ROW EXECUTE FUNCTION "general".set_edited_at();

-- =====================================================================
-- Table: device_profiles
-- =====================================================================

CREATE TABLE "general".device_profiles(
    id UUID                     PRIMARY KEY DEFAULT gen_random_uuid(),
    chirpstack_id UUID          NULL UNIQUE,
    name                        VARCHAR(100) NOT NULL,
    region                      VARCHAR(20) NOT NULL,
    mac_version                 "general".lorawan_mac_version NOT NULL,
    reg_params_revision         VARCHAR(20) NOT NULL,
    region_config_id            VARCHAR(50) NULL,
    adr_algorithm_id            VARCHAR(50) NOT NULL DEFAULT 'default',
    uplink_interval             INT NOT NULL DEFAULT 3600,
    device_status_req_interval  INT NOT NULL DEFAULT 1,
    supports_otaa               BOOLEAN NOT NULL DEFAULT true,
    flush_queue_on_activate     BOOLEAN NOT NULL DEFAULT true,
    auto_detect_measurements    BOOLEAN NOT NULL DEFAULT true,
    app_layer_params            JSONB NULL,
    sync_status                 "general".sync_status DEFAULT 'PENDING' NOT NULL,
    sync_error                  TEXT NULL,
    synced_at                   TIMESTAMPTZ NULL,
    created_at                  TIMESTAMPTZ DEFAULT now() NOT NULL,
    edited_at                   TIMESTAMPTZ DEFAULT now() NOT NULL
);

CREATE INDEX idx_device_profiles_sync_status
    ON "general".device_profiles (sync_status)
    WHERE sync_status <> 'SYNCEd';

CREATE TRIGGER trg_device_profiles_edited_at
    BEFORE UPDATE ON "general".device_profiles
    FOR EACH ROW EXECUTE FUNCTION "general".set_edited_at();

-- =====================================================================
-- Table: alarms
-- =====================================================================

CREATE TABLE "general".alarms (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    node_id         UUID NOT NULL,
    alarm           "general".alarm_type NOT NULL,
    threshold_min   NUMERIC(10, 2) NULL,
    threshold_max   NUMERIC(10, 2) NULL,
    severity        "general".alarm_severity NOT NULL,
    active          BOOLEAN DEFAULT true NOT NULL,
    created_at      TIMESTAMPTZ DEFAULT now() NOT NULL,
    edited_at       TIMESTAMPTZ DEFAULT now() NOT NULL,

    CONSTRAINT fk_alarms_nodes
        FOREIGN KEY (node_id)
        REFERENCES "general".nodes(id)
        ON DELETE CASCADE,

    CONSTRAINT chk_alarm_thresholds CHECK (
        alarm = 'POWER_OUTAGE'
        OR threshold_min IS NOT NULL
        OR threshold_max IS NOT NULL
    )
);

CREATE INDEX  idx_alarms_node_id ON "general".alarms (node_id);

CREATE TRIGGER trg_alarms_edited_at
    BEFORE UPDATE ON "general".alarms
    FOR EACH ROW EXECUTE FUNCTION "general".set_edited_at();

-- =====================================================================
-- Table: notifications
-- =====================================================================

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

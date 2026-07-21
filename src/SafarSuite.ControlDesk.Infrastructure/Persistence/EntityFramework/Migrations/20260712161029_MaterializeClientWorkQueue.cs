using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class MaterializeClientWorkQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE control.client_work_queue_items
                (
                    client_id uuid PRIMARY KEY
                        REFERENCES control.clients (client_id) ON DELETE CASCADE,
                    code character varying(32) NOT NULL,
                    name character varying(256) NOT NULL,
                    status character varying(32) NOT NULL,
                    action_label character varying(64) NOT NULL,
                    detail character varying(512) NOT NULL,
                    tab character varying(32) NOT NULL,
                    tone character varying(16) NOT NULL,
                    priority integer NOT NULL,
                    client_sort text GENERATED ALWAYS AS (lower(name)) STORED,
                    action_sort text GENERATED ALWAYS AS (lower(action_label)) STORED,
                    search_text text GENERATED ALWAYS AS
                    (
                        lower(code || ' ' || name || ' ' || status || ' '
                            || action_label || ' ' || detail || ' ' || tab)
                    ) STORED,
                    updated_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT ck_client_work_queue_priority CHECK (priority BETWEEN 0 AND 7),
                    CONSTRAINT ck_client_work_queue_tab CHECK
                    (
                        tab IN ('setup', 'billing', 'payments', 'access', 'cloud', 'overview')
                    ),
                    CONSTRAINT ck_client_work_queue_tone CHECK
                    (
                        tone IN ('ready', 'warning', 'neutral')
                    )
                );

                CREATE INDEX ix_client_work_queue_priority
                    ON control.client_work_queue_items (priority, code, client_id);
                CREATE INDEX ix_client_work_queue_client_sort
                    ON control.client_work_queue_items (client_sort, priority, code, client_id);
                CREATE INDEX ix_client_work_queue_action_sort
                    ON control.client_work_queue_items (action_sort, priority, code, client_id);
                CREATE INDEX ix_client_work_queue_lane_priority
                    ON control.client_work_queue_items (tab, priority, code, client_id);
                CREATE INDEX ix_client_work_queue_lane_client_sort
                    ON control.client_work_queue_items
                    (tab, client_sort, priority, code, client_id);
                CREATE INDEX ix_client_work_queue_lane_action_sort
                    ON control.client_work_queue_items
                    (tab, action_sort, priority, code, client_id);
                CREATE INDEX ix_client_work_queue_search
                    ON control.client_work_queue_items USING gin (search_text gin_trgm_ops);

                CREATE OR REPLACE FUNCTION control.refresh_client_work_queue(p_client_id uuid)
                RETURNS void
                LANGUAGE plpgsql
                AS $function$
                DECLARE
                    v_code text;
                    v_name text;
                    v_status text;
                    v_action_label text;
                    v_detail text;
                    v_tab text;
                    v_tone text := 'warning';
                    v_priority integer;
                    v_invoice_number text;
                    v_contact_count bigint := 0;
                    v_pending_count bigint := 0;
                    v_failed_count bigint := 0;
                BEGIN
                    SELECT client.code, client.display_name, client.status
                    INTO v_code, v_name, v_status
                    FROM control.clients AS client
                    WHERE client.client_id = p_client_id;

                    IF NOT FOUND THEN
                        DELETE FROM control.client_work_queue_items
                        WHERE client_id = p_client_id;
                        RETURN;
                    END IF;

                    IF v_status <> 'Active' THEN
                        v_priority := 0;
                    ELSIF NOT EXISTS
                    (
                        SELECT 1
                        FROM control.client_contacts AS contact
                        WHERE contact.client_id = p_client_id
                    ) THEN
                        v_priority := 1;
                    ELSIF NOT EXISTS
                    (
                        SELECT 1
                        FROM control.client_deployments AS deployment
                        WHERE deployment.client_id = p_client_id
                    ) THEN
                        v_priority := 2;
                    ELSIF NOT EXISTS
                    (
                        SELECT 1
                        FROM control.invoices AS invoice
                        WHERE invoice.client_id = p_client_id
                    ) THEN
                        v_priority := 3;
                    ELSIF EXISTS
                    (
                        SELECT 1
                        FROM control.invoices AS invoice
                        WHERE invoice.client_id = p_client_id
                          AND invoice.status IN ('Issued', 'PartiallyPaid')
                    ) THEN
                        v_priority := 4;
                    ELSIF NOT EXISTS
                    (
                        SELECT 1
                        FROM control.entitlement_snapshots AS entitlement
                        WHERE entitlement.client_id = p_client_id
                    )
                    AND EXISTS
                    (
                        SELECT 1
                        FROM control.invoices AS invoice
                        WHERE invoice.client_id = p_client_id
                          AND invoice.status = 'Paid'
                    ) THEN
                        v_priority := 5;
                    ELSIF EXISTS
                    (
                        SELECT 1
                        FROM control.cloud_outbox_messages AS message
                        WHERE message.client_id = p_client_id
                          AND message.status IN ('Pending', 'Failed')
                    ) THEN
                        v_priority := 6;
                    ELSE
                        v_priority := 7;
                    END IF;

                    v_action_label := CASE v_priority
                        WHEN 0 THEN 'Activate client'
                        WHEN 1 THEN 'Add contact'
                        WHEN 2 THEN 'Save deployment'
                        WHEN 3 THEN 'Draft invoice'
                        WHEN 4 THEN 'Record receipt'
                        WHEN 5 THEN 'Issue access'
                        WHEN 6 THEN 'Send to Cloud'
                        ELSE 'Review next action'
                    END;

                    v_tab := CASE v_priority
                        WHEN 0 THEN 'setup'
                        WHEN 1 THEN 'setup'
                        WHEN 2 THEN 'setup'
                        WHEN 3 THEN 'billing'
                        WHEN 4 THEN 'payments'
                        WHEN 5 THEN 'access'
                        WHEN 6 THEN 'cloud'
                        ELSE 'overview'
                    END;

                    CASE v_priority
                        WHEN 0 THEN
                            v_detail := v_status || ' master record';
                        WHEN 1 THEN
                            v_detail := 'No billing or support contact';
                        WHEN 2 THEN
                            v_detail := 'No local server profile';
                        WHEN 3 THEN
                            v_detail := 'No invoice voucher yet';
                        WHEN 4 THEN
                            SELECT invoice.number
                            INTO v_invoice_number
                            FROM control.invoices AS invoice
                            WHERE invoice.client_id = p_client_id
                              AND invoice.status IN ('Issued', 'PartiallyPaid')
                            ORDER BY invoice.issue_date, invoice.created_at_utc, invoice.invoice_id
                            LIMIT 1;
                            v_detail := v_invoice_number || ' due';
                        WHEN 5 THEN
                            SELECT invoice.number
                            INTO v_invoice_number
                            FROM control.invoices AS invoice
                            WHERE invoice.client_id = p_client_id
                              AND invoice.status = 'Paid'
                            ORDER BY invoice.issue_date, invoice.created_at_utc, invoice.invoice_id
                            LIMIT 1;
                            v_detail := v_invoice_number || ' is paid';
                        WHEN 6 THEN
                            SELECT
                                COUNT(*) FILTER (WHERE message.status = 'Pending'),
                                COUNT(*) FILTER (WHERE message.status = 'Failed')
                            INTO v_pending_count, v_failed_count
                            FROM control.cloud_outbox_messages AS message
                            WHERE message.client_id = p_client_id
                              AND message.status IN ('Pending', 'Failed');

                            IF v_failed_count > 0 THEN
                                v_detail := v_failed_count::text || ' failed cloud update'
                                    || CASE WHEN v_failed_count = 1 THEN '' ELSE 's' END;
                            ELSE
                                v_detail := v_pending_count::text || ' pending cloud update'
                                    || CASE WHEN v_pending_count = 1 THEN '' ELSE 's' END;
                            END IF;
                        ELSE
                            SELECT COUNT(*)
                            INTO v_contact_count
                            FROM control.client_contacts AS contact
                            WHERE contact.client_id = p_client_id;
                            v_detail := v_contact_count::text || ' contact'
                                || CASE WHEN v_contact_count = 1 THEN '' ELSE 's' END;
                    END CASE;

                    IF v_priority = 6 AND v_failed_count = 0 THEN
                        v_tone := 'ready';
                    ELSIF v_priority = 7 THEN
                        v_tone := 'ready';
                    END IF;

                    INSERT INTO control.client_work_queue_items
                    (
                        client_id,
                        code,
                        name,
                        status,
                        action_label,
                        detail,
                        tab,
                        tone,
                        priority,
                        updated_at_utc
                    )
                    VALUES
                    (
                        p_client_id,
                        v_code,
                        v_name,
                        v_status,
                        v_action_label,
                        v_detail,
                        v_tab,
                        v_tone,
                        v_priority,
                        clock_timestamp()
                    )
                    ON CONFLICT (client_id) DO UPDATE SET
                        code = EXCLUDED.code,
                        name = EXCLUDED.name,
                        status = EXCLUDED.status,
                        action_label = EXCLUDED.action_label,
                        detail = EXCLUDED.detail,
                        tab = EXCLUDED.tab,
                        tone = EXCLUDED.tone,
                        priority = EXCLUDED.priority,
                        updated_at_utc = EXCLUDED.updated_at_utc;
                END;
                $function$;

                DO $backfill$
                DECLARE
                    v_client_id uuid;
                BEGIN
                    FOR v_client_id IN
                        SELECT client_id FROM control.clients
                    LOOP
                        PERFORM control.refresh_client_work_queue(v_client_id);
                    END LOOP;
                END;
                $backfill$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS control.refresh_client_work_queue(uuid);
                DROP TABLE IF EXISTS control.client_work_queue_items;
                """);
        }
    }
}

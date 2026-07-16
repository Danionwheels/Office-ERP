\set ON_ERROR_STOP on

BEGIN;

WITH context AS
(
    SELECT c.client_id, cc.contract_id
    FROM control.clients AS c
    JOIN control.client_contracts AS cc ON cc.client_id = c.client_id
    ORDER BY c.created_at_utc, cc.created_at_utc
    LIMIT 1
),
generated AS
(
    SELECT
        series.value,
        md5('financial-volume-invoice-' || series.value)::uuid AS invoice_id,
        ('VOL-INV-' || lpad(series.value::text, 6, '0'))::varchar(40) AS invoice_number,
        (DATE '2015-01-01' + (series.value % 3650))::date AS issue_date,
        (TIMESTAMPTZ '2020-01-01 00:00:00+00' + series.value * INTERVAL '1 second') AS created_at_utc
    FROM generate_series(1, 20000) AS series(value)
)
INSERT INTO control.invoices
(
    invoice_id,
    client_id,
    contract_id,
    number,
    issue_date,
    due_date,
    currency_code,
    status,
    created_at_utc,
    amount_paid_amount,
    amount_paid_currency_code
)
SELECT
    generated.invoice_id,
    context.client_id,
    context.contract_id,
    generated.invoice_number,
    generated.issue_date,
    generated.issue_date + 30,
    'PKR',
    CASE WHEN generated.value % 3 = 0 THEN 'Paid' ELSE 'Issued' END,
    generated.created_at_utc,
    CASE WHEN generated.value % 3 = 0 THEN 100 ELSE 0 END,
    'PKR'
FROM generated
CROSS JOIN context
ON CONFLICT (invoice_id) DO NOTHING;

INSERT INTO control.invoice_lines
(
    description,
    amount,
    currency_code,
    invoice_id,
    line_type
)
SELECT
    'Financial read volume proof',
    100,
    'PKR',
    invoice.invoice_id,
    'Charge'
FROM control.invoices AS invoice
WHERE invoice.number LIKE 'VOL-INV-%'
  AND NOT EXISTS
  (
      SELECT 1
      FROM control.invoice_lines AS line
      WHERE line.invoice_id = invoice.invoice_id
  );

WITH context AS
(
    SELECT c.client_id
    FROM control.clients AS c
    JOIN control.client_contracts AS cc ON cc.client_id = c.client_id
    ORDER BY c.created_at_utc, cc.created_at_utc
    LIMIT 1
)
INSERT INTO control.payments
(
    payment_id,
    client_id,
    invoice_id,
    method,
    reference,
    amount,
    currency_code,
    received_on,
    recorded_at_utc,
    status,
    decision_note
)
SELECT
    md5('financial-volume-payment-' || invoice.number)::uuid,
    context.client_id,
    invoice.invoice_id,
    'ManualCash',
    ('VOL-PAY-' || right(invoice.number, 6))::varchar(80),
    100,
    'PKR',
    invoice.issue_date + 1,
    invoice.created_at_utc + INTERVAL '12 hours',
    'Approved',
    'Financial read volume proof'
FROM control.invoices AS invoice
CROSS JOIN context
WHERE invoice.number LIKE 'VOL-INV-%'
  AND invoice.status = 'Paid'
ON CONFLICT (payment_id) DO NOTHING;

INSERT INTO control.journal_entries
(
    journal_entry_id,
    entry_date,
    currency_code,
    source_type,
    source_reference,
    memo,
    status,
    created_at_utc,
    posted_at_utc,
    voided_at_utc,
    client_id,
    source_document_id
)
SELECT
    md5('financial-volume-invoice-journal-' || invoice.number)::uuid,
    invoice.issue_date,
    'PKR',
    'BillingInvoice',
    invoice.number,
    'Financial read volume invoice journal',
    'Posted',
    invoice.created_at_utc + INTERVAL '1 minute',
    invoice.created_at_utc + INTERVAL '1 minute',
    NULL,
    invoice.client_id,
    invoice.invoice_id
FROM control.invoices AS invoice
WHERE invoice.number LIKE 'VOL-INV-%'
ON CONFLICT (journal_entry_id) DO NOTHING;

INSERT INTO control.journal_entries
(
    journal_entry_id,
    entry_date,
    currency_code,
    source_type,
    source_reference,
    memo,
    status,
    created_at_utc,
    posted_at_utc,
    voided_at_utc,
    client_id,
    source_document_id
)
SELECT
    md5('financial-volume-payment-journal-' || payment.reference)::uuid,
    payment.received_on,
    'PKR',
    'PaymentReceipt',
    payment.reference,
    'Financial read volume payment journal',
    'Posted',
    payment.recorded_at_utc + INTERVAL '1 minute',
    payment.recorded_at_utc + INTERVAL '1 minute',
    NULL,
    payment.client_id,
    payment.payment_id
FROM control.payments AS payment
WHERE payment.reference LIKE 'VOL-PAY-%'
ON CONFLICT (journal_entry_id) DO NOTHING;

WITH ranked_accounts AS
(
    SELECT
        account.ledger_account_id,
        row_number() OVER (ORDER BY account.code, account.ledger_account_id) AS position
    FROM control.ledger_accounts AS account
    WHERE account.is_posting_account
      AND account.status = 'Active'
),
accounts AS
(
    SELECT
        (array_agg(ledger_account_id ORDER BY position))[1] AS debit_account_id,
        (array_agg(ledger_account_id ORDER BY position))[2] AS credit_account_id
    FROM ranked_accounts
),
proof_journals AS
(
    SELECT journal.journal_entry_id
    FROM control.journal_entries AS journal
    WHERE journal.memo LIKE 'Financial read volume %'
)
INSERT INTO control.journal_lines
(
    ledger_account_id,
    debit_amount,
    debit_currency_code,
    credit_amount,
    credit_currency_code,
    description,
    journal_entry_id
)
SELECT
    CASE WHEN side.is_debit THEN accounts.debit_account_id ELSE accounts.credit_account_id END,
    CASE WHEN side.is_debit THEN 100 ELSE 0 END,
    'PKR',
    CASE WHEN side.is_debit THEN 0 ELSE 100 END,
    'PKR',
    'Financial read volume proof',
    proof_journals.journal_entry_id
FROM proof_journals
CROSS JOIN accounts
CROSS JOIN (VALUES (TRUE), (FALSE)) AS side(is_debit)
WHERE accounts.debit_account_id IS NOT NULL
  AND accounts.credit_account_id IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1
      FROM control.journal_lines AS line
      WHERE line.journal_entry_id = proof_journals.journal_entry_id
  );

COMMIT;

ANALYZE control.invoices;
ANALYZE control.invoice_lines;
ANALYZE control.payments;
ANALYZE control.journal_entries;
ANALYZE control.journal_lines;

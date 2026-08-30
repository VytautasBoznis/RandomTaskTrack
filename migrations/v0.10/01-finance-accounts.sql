-- ─────────────────────────────────────────────────────────────────────────────
-- Accounts — where the money actually sits.
--
-- v0.07 derived one global cash figure from the whole ledger, which answers
-- "how much have I got" but not "how much is in which pot". This splits that
-- figure without giving up the thing that made it trustworthy.
--
-- **The balance stays derived.** There is no balance column here on purpose.
-- An account's balance is still SUM(its entries), and "set the balance to
-- 4,180" is a Balance adjustment entry for the difference — one row, dated,
-- visible in the ledger. A stored total would be the one number in the scope
-- that could disagree with everything else, and editing an old entry would
-- silently leave it wrong.
--
-- **A deposit moves its own money.** source_account_id and target_account_id
-- are what "take 10k out of Main for two years, then put it back in Savings
-- with the interest" means. Neither side writes a ledger entry: the principal
-- is subtracted from the source while the deposit is open and the maturity
-- value is added to the target once matures_on has passed, both derived in
-- FinanceProjector. Nothing to press on the maturity date, and deleting the
-- deposit undoes both halves for free.
--
-- Both columns are nullable because deposits that predate this migration were
-- funded by a hand-logged entry under the old rules. Attaching an account to
-- one of those retroactively would subtract the same money twice.
-- ─────────────────────────────────────────────────────────────────────────────

-- ─────────────────────────────────────────────────────────────────────────────
--   kind  1 = cash (a bank account), 2 = stock (a brokerage)
--
-- The kind does not change the arithmetic — every account carries a cash
-- balance and every account can hold shares. It records what the account is
-- *for*, which is what lets the holding form offer brokerages only and the
-- card say "12,840 in shares" rather than a bare number.
--
-- `currency` is the account's own, the one its balance is quoted in. Entries
-- against it may be in any currency; the balance converts them.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.fin_accounts
(
    id         uuid        NOT NULL PRIMARY KEY,
    name       text        NOT NULL,
    kind       int         NOT NULL,
    currency   text        NOT NULL REFERENCES tracker.fin_currencies (code),
    note       text        NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_fin_accounts_kind CHECK (kind IN (1, 2))
);

-- Two accounts called "Savings" is a typo every time, and the name is what the
-- dropdowns show.
CREATE UNIQUE INDEX IF NOT EXISTS ux_fin_accounts_name
    ON tracker.fin_accounts (lower(name));

-- One of each, seeded rather than left empty. Every ledger entry and every
-- holding needs an account, so a database with none would make "Log money" and
-- "Add holding" dead ends on first run. Fixed ids so the backfill below can
-- name them and re-running the migration is a no-op.
INSERT INTO tracker.fin_accounts (id, name, kind, currency, note)
VALUES ('00000000-0000-0000-0000-00000000ac01', 'Main account', 1, 'EUR', 'Everything logged before accounts existed.'),
       ('00000000-0000-0000-0000-00000000ac02', 'Investments', 2, 'EUR', 'Holdings added before accounts existed.')
ON CONFLICT (id) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Entries belong to an account. Added nullable, backfilled, then tightened —
-- the column has to be NOT NULL or "which account is this in" gets a third
-- answer that no screen knows how to show.
-- ─────────────────────────────────────────────────────────────────────────────
ALTER TABLE tracker.fin_entries
    ADD COLUMN IF NOT EXISTS account_id uuid NULL REFERENCES tracker.fin_accounts (id);

UPDATE tracker.fin_entries
SET account_id = '00000000-0000-0000-0000-00000000ac01'
WHERE account_id IS NULL;

ALTER TABLE tracker.fin_entries
    ALTER COLUMN account_id SET NOT NULL;

-- The balance query groups every entry by account and currency.
CREATE INDEX IF NOT EXISTS ix_fin_entries_account
    ON tracker.fin_entries (account_id);

-- ─────────────────────────────────────────────────────────────────────────────
-- Holdings belong to an account too, which is what makes a brokerage worth
-- what it holds.
--
-- The symbol is now unique per account rather than globally: the same ETF in a
-- taxable account and a pension is two positions, bought at different times for
-- different reasons, and forcing them into one row would make either one
-- unreportable. The price refresh asks the source once per distinct symbol and
-- writes the answer to every row that uses it.
-- ─────────────────────────────────────────────────────────────────────────────
ALTER TABLE tracker.fin_holdings
    ADD COLUMN IF NOT EXISTS account_id uuid NULL REFERENCES tracker.fin_accounts (id);

UPDATE tracker.fin_holdings
SET account_id = '00000000-0000-0000-0000-00000000ac02'
WHERE account_id IS NULL;

ALTER TABLE tracker.fin_holdings
    ALTER COLUMN account_id SET NOT NULL;

DROP INDEX IF EXISTS tracker.ux_fin_holdings_symbol;

CREATE UNIQUE INDEX IF NOT EXISTS ux_fin_holdings_account_symbol
    ON tracker.fin_holdings (account_id, lower(symbol));

-- ─────────────────────────────────────────────────────────────────────────────
-- Deposits know where the money came from and where it goes back to.
--
-- No ON DELETE clause on any of these three: an account with a balance, a
-- position or a deposit attached must not be deletable, and SET NULL would
-- quietly change a balance rather than refusing.
-- ─────────────────────────────────────────────────────────────────────────────
ALTER TABLE tracker.fin_deposits
    ADD COLUMN IF NOT EXISTS source_account_id uuid NULL REFERENCES tracker.fin_accounts (id),
    ADD COLUMN IF NOT EXISTS target_account_id uuid NULL REFERENCES tracker.fin_accounts (id);

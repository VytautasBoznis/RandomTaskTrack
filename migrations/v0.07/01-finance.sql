-- ─────────────────────────────────────────────────────────────────────────────
-- Finance — the first scope that is not task-shaped at all.
--
-- Recurring income and expenses, a ledger of what actually happened, stock
-- holdings priced on demand, expected dividends, term deposits, and targets to
-- draw on the projection graph. Everything entered by hand: no bank feed, no
-- broker sync, one button that pulls prices.
--
-- Two decisions run through the whole scope and are worth reading before
-- changing anything here.
--
-- **The projection is computed, not materialized.** That is the deliberate
-- reverse of task_tasks, where recurring instances are written ahead of time.
-- Tasks materialize because each instance has to be individually editable and
-- the horizon is 21 days. A 30-year projection is 360 buckets nobody edits that
-- change wholesale the moment one flow changes, so FinanceProjector derives them
-- on read and nothing below stores a future row.
--
-- **Cash and assets never double-count.** fin_entries is a cash ledger and
-- nothing else: income received, expenses paid. Deposits and holdings are
-- assets valued separately, and their *past* cash movements are already inside
-- the current cash balance — buying a share three years ago is why the cash is
-- lower now, so subtracting it again would count it twice. The only future cash
-- an asset produces is a deposit maturing and a dividend landing.
--
-- Money is numeric, never float. numeric(18,2) for amounts, (18,6) for share
-- quantities and FX rates, (18,4) for prices.
-- ─────────────────────────────────────────────────────────────────────────────

-- ─────────────────────────────────────────────────────────────────────────────
-- Currencies — a hand-maintained rate table, in keeping with the rest of the
-- scope. The base currency is the row with rate_to_base = 1.
--
-- Rates are current-value only, not a dated history. Everything here is a
-- "what is it worth now / what will it be worth" question, and a rate history
-- would only matter for restating the past, which this scope does not do.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.fin_currencies
(
    -- ISO 4217, uppercase. Short enough that the code is the key.
    code         text          NOT NULL PRIMARY KEY,
    name         text          NOT NULL,

    -- How many units of this currency make one unit of the base. USD at 1.08
    -- means 1 EUR = 1.08 USD, so converting USD to EUR divides.
    rate_to_base numeric(18, 6) NOT NULL,
    updated_at   timestamptz   NOT NULL DEFAULT now(),

    CONSTRAINT ck_fin_currencies_rate CHECK (rate_to_base > 0)
);

INSERT INTO tracker.fin_currencies (code, name, rate_to_base)
VALUES ('EUR', 'Euro', 1.0),
       ('USD', 'US dollar', 1.08)
ON CONFLICT (code) DO NOTHING;

-- ─────────────────────────────────────────────────────────────────────────────
-- Flows — the things that repeat. Income and expenses share a table because
-- they share every column and differ only in sign; splitting them would mean
-- two identical CRUD stacks and two halves of every projection query.
--
--   kind     1 = income, 2 = expense
--   cadence  1 = weekly, 2 = monthly, 3 = quarterly, 4 = yearly
--
-- Amounts are always positive; `kind` carries the direction.
--
-- Every cadence anchors on starts_on. day_of_month and month_of_year are
-- optional overrides for when the calendar day matters more than the anchor —
-- a salary on the 25th, an insurance premium every March. Leaving them null
-- means "same day of the month/year as starts_on", which is what you want for
-- most things and saves filling in two boxes to say nothing.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.fin_flows
(
    id            uuid           NOT NULL PRIMARY KEY,
    kind          int            NOT NULL,
    name          text           NOT NULL,
    amount        numeric(18, 2) NOT NULL,
    currency      text           NOT NULL REFERENCES tracker.fin_currencies (code),
    cadence       int            NOT NULL,

    day_of_month  int            NULL,
    month_of_year int            NULL,

    starts_on     date           NOT NULL,
    ends_on       date           NULL,

    -- Free text, not a lookup table. Categories are for grouping a chart, and
    -- an open column lets one be invented without a migration.
    category      text           NULL,

    -- Pausing rather than deleting: a flow that stops for three months keeps
    -- its history and comes back without being retyped.
    is_active     boolean        NOT NULL DEFAULT true,
    created_at    timestamptz    NOT NULL DEFAULT now(),
    updated_at    timestamptz    NOT NULL DEFAULT now(),

    -- Every branch is COALESCE-wrapped. A CHECK that evaluates to NULL counts as
    -- satisfied, which is how the first cut of ck_task_recurrences_shape let
    -- malformed rules straight through. Any new CHECK on a nullable column here
    -- needs the same treatment.
    CONSTRAINT ck_fin_flows_kind CHECK (kind IN (1, 2)),
    CONSTRAINT ck_fin_flows_cadence CHECK (cadence IN (1, 2, 3, 4)),
    CONSTRAINT ck_fin_flows_amount CHECK (amount > 0),
    CONSTRAINT ck_fin_flows_day CHECK (COALESCE(day_of_month, 1) BETWEEN 1 AND 31),
    CONSTRAINT ck_fin_flows_month CHECK (COALESCE(month_of_year, 1) BETWEEN 1 AND 12),
    CONSTRAINT ck_fin_flows_dates CHECK (COALESCE(ends_on, starts_on) >= starts_on)
);

-- The projection reads every active flow on every call, so the partial index is
-- the whole working set.
CREATE INDEX IF NOT EXISTS ix_fin_flows_active
    ON tracker.fin_flows (kind, starts_on)
    WHERE is_active;

-- ─────────────────────────────────────────────────────────────────────────────
-- Entries — the cash ledger. What actually happened, as opposed to what
-- fin_flows says is supposed to happen.
--
-- This is what makes current cash a derived number instead of one typed into a
-- box and left to rot. Seed it with a single "Opening balance" entry and log
-- from there.
--
-- flow_id is a soft link back to the recurring definition that this entry is an
-- instance of — set when you tick off "yes, the rent went out" — and is
-- ON DELETE SET NULL because deleting the definition must not erase the record
-- that the money moved.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.fin_entries
(
    id          uuid           NOT NULL PRIMARY KEY,
    flow_id     uuid           NULL REFERENCES tracker.fin_flows (id) ON DELETE SET NULL,
    kind        int            NOT NULL,
    name        text           NOT NULL,
    amount      numeric(18, 2) NOT NULL,
    currency    text           NOT NULL REFERENCES tracker.fin_currencies (code),
    occurred_on date           NOT NULL,
    category    text           NULL,
    note        text           NULL,
    created_at  timestamptz    NOT NULL DEFAULT now(),

    CONSTRAINT ck_fin_entries_kind CHECK (kind IN (1, 2)),
    CONSTRAINT ck_fin_entries_amount CHECK (amount > 0)
);

-- Two reads: the ledger list (newest first) and the monthly actual-vs-planned
-- rollup, which groups by month over the same column.
CREATE INDEX IF NOT EXISTS ix_fin_entries_occurred
    ON tracker.fin_entries (occurred_on DESC);

CREATE INDEX IF NOT EXISTS ix_fin_entries_flow
    ON tracker.fin_entries (flow_id)
    WHERE flow_id IS NOT NULL;

-- ─────────────────────────────────────────────────────────────────────────────
-- Holdings — one row per symbol, carrying the last price the button pulled.
--
-- `symbol` is stored in the price source's own vocabulary (Yahoo wants `AAPL`
-- for Nasdaq and `ASML.AS` for Amsterdam), the same bargain recipe_families
-- makes with cuisine codes: a second source would need its own mapping anyway,
-- so there is no pretence of a neutral ticker namespace here.
--
-- last_price is nullable and stays null until the first successful refresh —
-- a holding added offline is still a holding, it just has no value yet.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.fin_holdings
(
    id            uuid           NOT NULL PRIMARY KEY,
    symbol        text           NOT NULL,
    name          text           NULL,
    currency      text           NOT NULL REFERENCES tracker.fin_currencies (code),
    last_price    numeric(18, 4) NULL,
    last_price_at timestamptz    NULL,
    created_at    timestamptz    NOT NULL DEFAULT now(),

    CONSTRAINT ck_fin_holdings_price CHECK (COALESCE(last_price, 0) >= 0)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_fin_holdings_symbol
    ON tracker.fin_holdings (lower(symbol));

-- ─────────────────────────────────────────────────────────────────────────────
-- Trades — buys and sells. The position is the sum, never a stored total:
-- storing both invites them to disagree, and "corrections I can do manually"
-- means editing the trade that was wrong and having the position follow.
--
--   side  1 = buy, 2 = sell
--
-- quantity is always positive; `side` carries the sign. Fractional shares are
-- real, hence numeric(18,6) rather than an integer count.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.fin_trades
(
    id         uuid           NOT NULL PRIMARY KEY,
    holding_id uuid           NOT NULL REFERENCES tracker.fin_holdings (id) ON DELETE CASCADE,
    side       int            NOT NULL,
    quantity   numeric(18, 6) NOT NULL,
    price      numeric(18, 4) NOT NULL,

    -- Commission, stamp duty, whatever the broker took. Part of the cost basis,
    -- not of the price.
    fee        numeric(18, 2) NOT NULL DEFAULT 0,
    traded_on  date           NOT NULL,
    note       text           NULL,
    created_at timestamptz    NOT NULL DEFAULT now(),

    CONSTRAINT ck_fin_trades_side CHECK (side IN (1, 2)),
    CONSTRAINT ck_fin_trades_quantity CHECK (quantity > 0),
    CONSTRAINT ck_fin_trades_price CHECK (price >= 0),
    CONSTRAINT ck_fin_trades_fee CHECK (fee >= 0)
);

-- Positions are summed per holding; the trade list under a holding is newest
-- first.
CREATE INDEX IF NOT EXISTS ix_fin_trades_holding
    ON tracker.fin_trades (holding_id, traded_on DESC);

-- ─────────────────────────────────────────────────────────────────────────────
-- Dividends — what you *expect* to be paid, entered by hand. Same cadence
-- vocabulary as fin_flows.
--
-- holding_id is optional. Attached, it documents which position pays it;
-- detached, it still projects, which is what lets a payer that is not tracked
-- as a holding still show up in the cash line.
--
-- These are expectations, not receipts. A dividend that actually landed is a
-- fin_entries row like any other income.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.fin_dividends
(
    id            uuid           NOT NULL PRIMARY KEY,
    holding_id    uuid           NULL REFERENCES tracker.fin_holdings (id) ON DELETE CASCADE,
    name          text           NOT NULL,

    -- Per payment, not per year. A quarterly dividend is four of these.
    amount        numeric(18, 2) NOT NULL,
    currency      text           NOT NULL REFERENCES tracker.fin_currencies (code),
    cadence       int            NOT NULL,
    day_of_month  int            NULL,
    month_of_year int            NULL,
    starts_on     date           NOT NULL,
    ends_on       date           NULL,
    is_active     boolean        NOT NULL DEFAULT true,
    created_at    timestamptz    NOT NULL DEFAULT now(),
    updated_at    timestamptz    NOT NULL DEFAULT now(),

    CONSTRAINT ck_fin_dividends_cadence CHECK (cadence IN (1, 2, 3, 4)),
    CONSTRAINT ck_fin_dividends_amount CHECK (amount > 0),
    CONSTRAINT ck_fin_dividends_day CHECK (COALESCE(day_of_month, 1) BETWEEN 1 AND 31),
    CONSTRAINT ck_fin_dividends_month CHECK (COALESCE(month_of_year, 1) BETWEEN 1 AND 12),
    CONSTRAINT ck_fin_dividends_dates CHECK (COALESCE(ends_on, starts_on) >= starts_on)
);

CREATE INDEX IF NOT EXISTS ix_fin_dividends_active
    ON tracker.fin_dividends (starts_on)
    WHERE is_active;

-- ─────────────────────────────────────────────────────────────────────────────
-- Deposits — money parked at a known rate. Unlike a stock, the growth is
-- contractual rather than a guess, so the projection can value these exactly.
--
--   compounding  1 = simple, 2 = monthly, 3 = annual
--
-- annual_rate is a percentage: 4.25 means 4.25%, not 0.0425. Storing the number
-- as it appears on the bank's page is one less place to drop a factor of 100.
--
-- matures_on null is an open-ended savings account: it keeps accruing and never
-- returns to cash on its own.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.fin_deposits
(
    id          uuid           NOT NULL PRIMARY KEY,
    name        text           NOT NULL,
    principal   numeric(18, 2) NOT NULL,
    currency    text           NOT NULL REFERENCES tracker.fin_currencies (code),
    annual_rate numeric(9, 4)  NOT NULL,
    compounding int            NOT NULL DEFAULT 3,
    opened_on   date           NOT NULL,
    matures_on  date           NULL,
    note        text           NULL,
    created_at  timestamptz    NOT NULL DEFAULT now(),
    updated_at  timestamptz    NOT NULL DEFAULT now(),

    CONSTRAINT ck_fin_deposits_principal CHECK (principal > 0),
    CONSTRAINT ck_fin_deposits_rate CHECK (annual_rate >= 0),
    CONSTRAINT ck_fin_deposits_compounding CHECK (compounding IN (1, 2, 3)),
    CONSTRAINT ck_fin_deposits_dates CHECK (COALESCE(matures_on, opened_on) >= opened_on)
);

-- ─────────────────────────────────────────────────────────────────────────────
-- Targets — the marks drawn on the projection.
--
-- Both columns are nullable on purpose, which gives three useful shapes:
--   amount only            a goal line across the chart ("100k")
--   target_on only         a dated milestone ("mortgage ends")
--   both                   a point to hit ("100k by 2030") — the real one
--
-- At least one has to be set, or there is nothing to draw.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.fin_targets
(
    id         uuid           NOT NULL PRIMARY KEY,
    label      text           NOT NULL,
    target_on  date           NULL,
    amount     numeric(18, 2) NULL,
    note       text           NULL,
    created_at timestamptz    NOT NULL DEFAULT now(),

    CONSTRAINT ck_fin_targets_something CHECK (target_on IS NOT NULL OR amount IS NOT NULL)
);

CREATE INDEX IF NOT EXISTS ix_fin_targets_date
    ON tracker.fin_targets (target_on);

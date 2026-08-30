-- ─────────────────────────────────────────────────────────────────────────────
-- Debts — the other side of the balance sheet.
--
-- Everything in the scope so far has been something you own. This is the first
-- thing you owe, and it is what makes "I am saving for a flat" a question the
-- projection can actually answer: the deposit builds up, the downpayment goes
-- out, the rent flow ends, and a mortgage payment starts in the same month.
--
-- Three decisions run through this and are worth reading before changing it.
--
-- **The balance is amortised, not stored.** There is no outstanding_balance
-- column, for the same reason fin_accounts has no balance column. What you owe
-- on a given month is the opening principal run forward through the schedule:
-- interest accrues on the balance, the payment covers it, the remainder comes
-- off the principal. That is contractual arithmetic, not a guess — the same
-- footing fin_deposits sits on, pointed the other way. FinanceProjector.Amortise
-- is the one place it happens.
--
-- **The payoff date is derived; ends_on is the contract.** Storing "paid off in
-- 2051" and then letting someone throw 10k at it would leave the stored date
-- lying. So ends_on records what the bank's paperwork says and nothing more,
-- and the month the balance actually reaches zero is computed. The gap between
-- them is the entire point of the overpayments below: pay a chunk, watch the
-- payoff date move. ends_on still caps the schedule — a balance left standing
-- on that date is a balloon (a lease residual, say), reported rather than
-- silently paid off or silently paid forever.
--
-- **A debt is not the thing it bought.** asset_value is what the mortgage got
-- you, carried on the debt rather than in an assets table, because a tracker
-- that needs one of those needs valuations and disposals and a whole scope of
-- its own. Held flat: the projection does not appreciate property, the same way
-- it holds shares at their last price unless you type an assumption. Without
-- it, signing for a flat reads as a 180k loss on the chart, which is the kind
-- of true-but-useless number that makes people stop looking at the chart.
--
--   fin_debts             what you owe and what it bought
--   fin_debt_payments     lump sums off the principal, over and above the
--                         monthly payment
--
-- Money is numeric, never float — numeric(18,2) for amounts, (9,4) for rates,
-- as everywhere else in the scope.
-- ─────────────────────────────────────────────────────────────────────────────

-- ─────────────────────────────────────────────────────────────────────────────
-- annual_rate is a percentage: 3.25 means 3.25%, not 0.0325. Stored as the
-- lender writes it, one less place to drop a factor of 100. Zero is legal and
-- useful — it degenerates the schedule to a flat drawdown, which is what an
-- interest-free instalment plan or a simple lease actually is.
--
-- payment is monthly. Not a cadence column: mortgages, car loans and leases are
-- monthly, the projection buckets by month anyway, and a weekly amortisation
-- schedule would be four columns of machinery for a debt nobody has.
--
-- The two account columns are the mirror of fin_deposits.source_account_id.
-- Named, they move their own money on starts_on and no ledger entry should be
-- logged for either:
--
--   down_payment_account_id    the deposit you put down leaves this account
--   disburses_to_account_id    the borrowed principal lands in this account
--
-- Both nullable, and independently so, because the two shapes differ. A
-- mortgage has a downpayment but no disbursement — the bank pays the seller and
-- the money never touches your account. A car loan has both. A debt you took
-- out three years ago should name neither: that cash moved long ago and is
-- already inside today's balance, so deriving it again would take it out twice.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.fin_debts
(
    id                      uuid           NOT NULL PRIMARY KEY,
    name                    text           NOT NULL,

    -- What was borrowed, at origination. Not what is left — that is derived.
    principal               numeric(18, 2) NOT NULL,
    currency                text           NOT NULL REFERENCES tracker.fin_currencies (code),
    annual_rate             numeric(9, 4)  NOT NULL,
    payment                 numeric(18, 2) NOT NULL,

    -- First payment. Also the day the downpayment leaves and the principal
    -- lands, for debts that name accounts for either.
    starts_on               date           NOT NULL,

    -- The contractual last payment. Null means "until it is paid off", which is
    -- the honest shape for a loan you intend to clear early.
    ends_on                 date           NULL,

    -- What the borrowing bought, at today's value. Null for a debt that bought
    -- nothing you would count — a student loan, a credit card balance.
    asset_value             numeric(18, 2) NULL,

    down_payment            numeric(18, 2) NULL,
    down_payment_account_id uuid           NULL REFERENCES tracker.fin_accounts (id),
    disburses_to_account_id uuid           NULL REFERENCES tracker.fin_accounts (id),

    note                    text           NULL,
    created_at              timestamptz    NOT NULL DEFAULT now(),
    updated_at              timestamptz    NOT NULL DEFAULT now(),

    -- Every branch is COALESCE-wrapped. A CHECK that evaluates to NULL counts as
    -- satisfied, which is how the first cut of ck_task_recurrences_shape let
    -- malformed rules straight through. Any new CHECK on a nullable column here
    -- needs the same treatment.
    CONSTRAINT ck_fin_debts_principal CHECK (principal > 0),
    CONSTRAINT ck_fin_debts_rate CHECK (annual_rate >= 0),
    CONSTRAINT ck_fin_debts_payment CHECK (payment > 0),
    CONSTRAINT ck_fin_debts_dates CHECK (COALESCE(ends_on, starts_on) >= starts_on),
    CONSTRAINT ck_fin_debts_asset CHECK (COALESCE(asset_value, 0) >= 0),
    CONSTRAINT ck_fin_debts_down_payment CHECK (COALESCE(down_payment, 0) >= 0),

    -- An account to take the downpayment out of, with no downpayment to take,
    -- is a half-filled form rather than a fact. The other way round is fine:
    -- that is a downpayment you made yourself and only want recorded.
    CONSTRAINT ck_fin_debts_down_payment_shape
        CHECK (down_payment_account_id IS NULL OR down_payment IS NOT NULL)
);

-- The projection walks every debt on every call, and the schedule starts at
-- starts_on.
CREATE INDEX IF NOT EXISTS ix_fin_debts_starts
    ON tracker.fin_debts (starts_on);

-- ─────────────────────────────────────────────────────────────────────────────
-- Overpayments — money off the principal on top of the monthly payment.
--
-- The same relationship fin_trades has with fin_holdings: the parent carries
-- the terms, the children carry the events, and the number everyone cares about
-- is the sum rather than a stored total. Correcting a mistyped chunk corrects
-- the balance, the payoff date and the interest saved for free, which is not
-- true of anything that writes a running balance down.
--
-- account_id follows the same rule as the parent's two: named, the cash leaves
-- that account on paid_on and no entry should be logged for it; null, you
-- logged it yourself or it predates the debt being tracked here.
-- ─────────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tracker.fin_debt_payments
(
    id         uuid           NOT NULL PRIMARY KEY,
    debt_id    uuid           NOT NULL REFERENCES tracker.fin_debts (id) ON DELETE CASCADE,
    amount     numeric(18, 2) NOT NULL,
    paid_on    date           NOT NULL,
    account_id uuid           NULL REFERENCES tracker.fin_accounts (id),
    note       text           NULL,
    created_at timestamptz    NOT NULL DEFAULT now(),

    CONSTRAINT ck_fin_debt_payments_amount CHECK (amount > 0)
);

-- Summed per debt, and the schedule applies them in date order.
CREATE INDEX IF NOT EXISTS ix_fin_debt_payments_debt
    ON tracker.fin_debt_payments (debt_id, paid_on);

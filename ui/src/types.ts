// Mirrors the DTOs in RandomTaskTrack.Data. ASP.NET serialises PascalCase
// properties as camelCase, and DateOnly/TimeOnly as "yyyy-MM-dd" / "HH:mm:ss".

/** TaskItemStatus: 1 Pending, 2 Done, 3 Skipped. */
export type TaskItemStatus = 1 | 2 | 3;

export const TaskStatus = { Pending: 1, Done: 2, Skipped: 3 } as const;

export interface Session {
  jwtToken: string;
  userId: string;
  email: string;
  expiresAt: string;
}

export interface TaskListItem {
  id: string;
  domainId: number;
  domainCode: string;
  domainName: string;
  recurrenceId: string | null;
  title: string;
  notes: string | null;
  data: string;
  dueOn: string;
  dueTime: string | null;
  status: TaskItemStatus;
  completedAt: string | null;
}

export interface TaskDomain {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
}

/** What the task form sends. `data` (the domain payload) is left to the server. */
export interface TaskDraft {
  domainId: number;
  title: string;
  notes: string | null;
  dueOn: string;
  dueTime: string | null;
}

export interface DomainStreak {
  domainId: number;
  domainCode: string;
  domainName: string;
  completedLast7Days: number;
  skippedLast7Days: number;
  pendingOverdue: number;
  lastCompletedAt: string | null;
}

export interface Dashboard {
  today: string;
  overdue: TaskListItem[];
  dueToday: TaskListItem[];
  upcoming: TaskListItem[];
  completedToday: TaskListItem[];
  streaks: DomainStreak[];
}

/** RecurrenceRuleType: 1 every N days, 2 days of week, 3 day of month. */
export type RecurrenceRuleType = 1 | 2 | 3;

export const RuleType = { IntervalDays: 1, DaysOfWeek: 2, DayOfMonth: 3 } as const;

/** RecurrenceAnchorMode: 1 from schedule, 2 from completion. */
export type RecurrenceAnchorMode = 1 | 2;

export const AnchorMode = { FromSchedule: 1, FromCompletion: 2 } as const;

export interface Recurrence {
  id: string;
  domainId: number;
  domainCode: string;
  title: string;
  notes: string | null;
  data: string;
  ruleType: RecurrenceRuleType;
  intervalDays: number | null;
  daysOfWeek: number[] | null;
  dayOfMonth: number | null;
  anchorMode: RecurrenceAnchorMode;
  timeOfDay: string | null;
  startsOn: string;
  endsOn: string | null;
  isActive: boolean;
  lastDueOn: string | null;
}

/** Update ignores `domainId` and `startsOn`; the form disables them when editing. */
export interface RecurrenceDraft {
  domainId: number;
  title: string;
  notes: string | null;
  ruleType: RecurrenceRuleType;
  intervalDays: number | null;
  daysOfWeek: number[] | null;
  dayOfMonth: number | null;
  anchorMode: RecurrenceAnchorMode;
  timeOfDay: string | null;
  /** Null lets the server start it today, in the scheduler's timezone. */
  startsOn: string | null;
  endsOn: string | null;
}

export interface CompletionLogItem {
  id: string;
  taskId: string;
  domainId: number;
  domainCode: string;
  title: string;
  status: TaskItemStatus;
  plannedData: string;
  actualData: string;
  note: string | null;
  dueOn: string;
  completedAt: string;
}

export interface RecipeFamily {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
  sortOrder: number;
}

export interface RecipeIngredient {
  item: string;
  amount: string | null;
}

export interface WeeklyDish {
  pickId: string;
  weekOf: string;
  recipeId: string;
  title: string;
  familyName: string | null;
  imageUrl: string | null;
  sourceUrl: string | null;
  readyMinutes: number | null;
  servings: number | null;
  ingredients: RecipeIngredient[];
  steps: string[];
  rating: number | null;
  notes: string;
  tags: string[];
  /** Set once the dish is on the board. */
  taskId: string | null;
}

/**
 * Mirrors RecipeTags.NotPicked. An ordinary tag the rotation happens to read:
 * a dish carrying it is never offered, but stays searchable like any other.
 */
export const NOT_PICKED = 'not picked';

/** The bulk local catalog that targeted search reads from. */
export interface CatalogStatus {
  /** Recipes currently in the catalog. Survives restarts — it's a row count. */
  loaded: number;
  /** Rows in the source file, for progress and for "what am I about to pull". */
  sourceRows: number;
  isRunning: boolean;
  rowsRead: number;
  /** New recipes the last run added. A re-run usually adds 0. */
  rowsAdded: number;
  /** Set once a run has finished in this server process. */
  finishedAt: string | null;
  error: string | null;
}

/** A search result, straight from the source and not saved yet. */
export interface RecipeCandidate {
  externalId: string;
  title: string;
  imageUrl: string | null;
  sourceUrl: string | null;
  readyMinutes: number | null;
  servings: number | null;
  ingredients: RecipeIngredient[];
  steps: string[];
}

/** A library row. `weekOf` null means saved but never cooked. */
export interface RecipeHistoryItem {
  recipeId: string;
  title: string;
  familyName: string | null;
  imageUrl: string | null;
  sourceUrl: string | null;
  readyMinutes: number | null;
  servings: number | null;
  weekOf: string | null;
  rating: number | null;
  notes: string;
  tags: string[];
}

/** Every field is optional; the server reads null as "leave alone". */
export interface RecipeMetaDraft {
  rating?: number | null;
  clearRating?: boolean;
  notes?: string;
  tags?: string[];
}

export interface Note {
  id: string;
  title: string;
  /** Markdown, rendered client-side. */
  content: string;
  createdAt: string;
  updatedAt: string;
}

/** Update reads null as "leave alone"; the form always sends both fields. */
export interface NoteDraft {
  title: string;
  content: string;
}

export interface ConversationListItem {
  id: string;
  title: string;
  domainId: number | null;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
}

/** Role is "user" or "assistant"; tool turns are filtered out server-side. */
export interface ChatMessage {
  id: string;
  seq: number;
  role: string;
  content: string | null;
  toolCalls: string | null;
  createdAt: string;
}

export interface ConversationDetail {
  id: string;
  title: string;
  domainId: number | null;
  messages: ChatMessage[];
}

export interface AppliedToolCall {
  name: string;
  input: string;
  result: string;
  isError: boolean;
}

export interface ChatReply {
  conversationId: string;
  reply: string;
  appliedToolCalls: AppliedToolCall[];
  inputTokens: number;
  outputTokens: number;
}

// ── Finance ──────────────────────────────────────────────────────────────────
// Amounts are decimals on the server and arrive as JSON numbers. Anything named
// *Base is already converted to the base currency; everything else is in the
// instrument's own currency.

/** FinanceFlowKind: 1 Income, 2 Expense. */
export type FlowKind = 1 | 2;

/** FinanceCadence: 1 Weekly, 2 Monthly, 3 Quarterly, 4 Yearly. */
export type Cadence = 1 | 2 | 3 | 4;

/** TradeSide: 1 Buy, 2 Sell. */
export type TradeSide = 1 | 2;

/** DepositCompounding: 1 Simple, 2 Monthly, 3 Annual. */
export type Compounding = 1 | 2 | 3;

export const FlowKinds = { Income: 1, Expense: 2 } as const;
export const Cadences = { Weekly: 1, Monthly: 2, Quarterly: 3, Yearly: 4 } as const;
export const TradeSides = { Buy: 1, Sell: 2 } as const;

export const CADENCE_LABELS: Record<Cadence, string> = {
  1: 'Weekly',
  2: 'Monthly',
  3: 'Quarterly',
  4: 'Yearly',
};

export const COMPOUNDING_LABELS: Record<Compounding, string> = {
  1: 'Simple',
  2: 'Monthly',
  3: 'Annual',
};

export interface Currency {
  code: string;
  name: string;
  rateToBase: number;
  updatedAt: string;
}

export interface FinanceFlow {
  id: string;
  kind: FlowKind;
  name: string;
  amount: number;
  currency: string;
  cadence: Cadence;
  dayOfMonth: number | null;
  monthOfYear: number | null;
  startsOn: string;
  endsOn: string | null;
  category: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface LedgerEntry {
  id: string;
  flowId: string | null;
  kind: FlowKind;
  name: string;
  amount: number;
  currency: string;
  occurredOn: string;
  category: string | null;
  note: string | null;
  createdAt: string;
}

export interface Trade {
  id: string;
  holdingId: string;
  side: TradeSide;
  quantity: number;
  price: number;
  fee: number;
  tradedOn: string;
  note: string | null;
  createdAt: string;
}

/** The stored row. The overview returns {@link Position} instead — trades folded in. */
export interface Holding {
  id: string;
  symbol: string;
  name: string | null;
  currency: string;
  lastPrice: number | null;
  lastPriceAt: string | null;
  createdAt: string;
}

export interface Position {
  id: string;
  symbol: string;
  name: string | null;
  currency: string;
  lastPrice: number | null;
  lastPriceAt: string | null;
  quantity: number;
  costBasis: number;
  marketValue: number | null;
  marketValueBase: number | null;
  trades: Trade[];
}

export interface Dividend {
  id: string;
  holdingId: string | null;
  name: string;
  amount: number;
  currency: string;
  cadence: Cadence;
  dayOfMonth: number | null;
  monthOfYear: number | null;
  startsOn: string;
  endsOn: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Deposit {
  id: string;
  name: string;
  principal: number;
  currency: string;
  /** A percentage as the bank writes it: 4.25 means 4.25%. */
  annualRate: number;
  compounding: Compounding;
  openedOn: string;
  maturesOn: string | null;
  note: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface FinanceTarget {
  id: string;
  label: string;
  targetOn: string | null;
  amount: number | null;
  note: string | null;
  createdAt: string;
}

export interface FinanceOverview {
  today: string;
  baseCurrency: string;
  cashBase: number;
  depositsBase: number;
  stocksBase: number;
  netWorthBase: number;
  monthlyIncomeBase: number;
  monthlyExpenseBase: number;
  /** The totals are short by these — say so rather than showing a flat number. */
  hasUnpricedHoldings: boolean;
  flows: FinanceFlow[];
  positions: Position[];
  deposits: Deposit[];
  dividends: Dividend[];
  targets: FinanceTarget[];
  currencies: Currency[];
}

export interface ProjectionPoint {
  /** First of the month, YYYY-MM-DD. */
  month: string;
  /** Ledger actuals rather than projected flows. Balances are null before today. */
  isActual: boolean;
  income: number;
  expenses: number;
  net: number;
  cash: number | null;
  deposits: number | null;
  stocks: number | null;
  netWorth: number | null;
}

/** What the forms send. Nulls mean "leave unchanged" on update. */
export interface FlowDraft {
  kind: FlowKind;
  name: string;
  amount: number;
  currency: string;
  cadence: Cadence;
  dayOfMonth: number | null;
  monthOfYear: number | null;
  startsOn: string;
  endsOn: string | null;
  category: string | null;
}

export interface EntryDraft {
  kind: FlowKind;
  name: string;
  amount: number;
  currency: string;
  occurredOn: string;
  flowId: string | null;
  category: string | null;
  note: string | null;
}

export interface HoldingDraft {
  symbol: string;
  name: string | null;
  currency: string;
}

export interface TradeDraft {
  holdingId: string;
  side: TradeSide;
  quantity: number;
  price: number;
  fee: number | null;
  tradedOn: string;
  note: string | null;
}

export interface DividendDraft {
  holdingId: string | null;
  name: string;
  amount: number;
  currency: string;
  cadence: Cadence;
  dayOfMonth: number | null;
  monthOfYear: number | null;
  startsOn: string;
  endsOn: string | null;
}

export interface DepositDraft {
  name: string;
  principal: number;
  currency: string;
  annualRate: number;
  compounding: Compounding;
  openedOn: string;
  maturesOn: string | null;
  note: string | null;
}

export interface TargetDraft {
  label: string;
  targetOn: string | null;
  amount: number | null;
  note: string | null;
}

export interface PriceRefreshResult {
  updatedHoldings: number;
  updatedCurrencies: number;
  /** Symbols the source had no price for. The stale price is kept. */
  failed: string[];
}

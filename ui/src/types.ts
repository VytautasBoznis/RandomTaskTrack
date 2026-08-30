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

/** A learning path with something on the board, as one row. `goalId` is null on
 *  a credential renewal — those sit on no path. */
export interface DashboardLearning {
  goalId: string | null;
  title: string;
  count: number;
  nextDueOn: string;
}

export interface Dashboard {
  today: string;
  overdue: TaskListItem[];
  dueToday: TaskListItem[];
  upcoming: TaskListItem[];
  completedToday: TaskListItem[];
  /** Kept out of the buckets above — the server files these here instead. */
  learning: DashboardLearning[];
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

// ── Plants ───────────────────────────────────────────────────────────────────

/** PlantKind: 1 a plant you have, 2 a seed packet not sown yet. */
export type PlantKind = 1 | 2;

export const PlantKinds = { Plant: 1, SeedPacket: 2 } as const;

/** One line of the suggested care schedule, and what turns into a recurrence. */
export interface PlantCareTask {
  title: string;
  intervalDays: number;
  notes: string;
}

/** One step of a sowing plan, as an offset in days from the sowing itself. */
export interface PlantSowingStep {
  title: string;
  dayOffset: number;
  notes: string;
}

/** Filled in for a seed packet only. */
export interface PlantSowing {
  method: string;
  sowWindow: string;
  sowDepthMm: number | null;
  spacingCm: number | null;
  germinationDays: number | null;
  daysToHarvest: number | null;
  startIndoors: boolean;
  notes: string;
  steps: PlantSowingStep[];
}

/** A photo, which is also a stage. The bytes come from a separate authed call. */
export interface PlantPhoto {
  id: string;
  plantId: string;
  mediaType: string;
  /** What the AI made of it — "first true leaves" — or whatever was typed. */
  stage: string;
  note: string;
  takenOn: string;
  createdAt: string;
}

/** The lookup's answer. Every field is prose to read; empty means "nothing useful". */
export interface PlantProfile {
  speciesCommon: string | null;
  speciesLatin: string | null;
  /** "high" | "medium" | "low" — shown when it is not high. */
  confidence: string;
  reasoning: string;
  summary: string;
  light: string;
  water: string;
  humidity: string;
  temperature: string;
  soil: string;
  feeding: string;
  repotting: string;
  toxicity: string;
  commonProblems: string[];
  careTasks: PlantCareTask[];
  /** Seed packets only. */
  sowing: PlantSowing | null;
}

export interface Plant {
  id: string;
  kind: PlantKind;
  name: string;
  location: string | null;
  species: string | null;
  latinName: string | null;
  acquiredOn: string | null;
  notes: string;
  /** The free text the identification was made from. */
  description: string;
  /** Null until a lookup has succeeded — a plant can exist without one. */
  profile: PlantProfile | null;
  researchedAt: string | null;
  researchModel: string | null;
  /** Pending care tasks, soonest first. */
  tasks: TaskListItem[];
  /** The care schedule, paused entries included. */
  recurrences: Recurrence[];
  /** Newest first — the stage history. */
  photos: PlantPhoto[];
  createdAt: string;
  updatedAt: string;
}

/** What the add form sends. The lookup runs on the photo and the description. */
export interface PlantDraft {
  kind: PlantKind;
  name: string;
  location: string | null;
  description: string | null;
  /** Raw base64, no data: prefix. Downscaled in the browser first. */
  imageBase64: string | null;
  mediaType: string | null;
  acquiredOn: string | null;
  notes: string | null;
}

/** Every field optional; the server reads null as "leave alone". */
export interface PlantEdit {
  kind?: PlantKind;
  name?: string;
  location?: string | null;
  species?: string | null;
  latinName?: string | null;
  acquiredOn?: string | null;
  notes?: string;
}

/** What the photo uploader sends. A null stage asks the AI to read one. */
export interface PlantPhotoDraft {
  imageBase64: string;
  mediaType: string;
  takenOn: string | null;
  stage: string | null;
  note: string | null;
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

/** AccountKind: 1 Cash (a bank account), 2 Stock (a brokerage or pension). */
export type AccountKind = 1 | 2;

export const AccountKinds = { Cash: 1, Stock: 2 } as const;

export const ACCOUNT_KIND_LABELS: Record<AccountKind, string> = {
  1: 'Cash',
  2: 'Stocks',
};

export interface Currency {
  code: string;
  name: string;
  rateToBase: number;
  updatedAt: string;
}

/** The stored row. The overview returns {@link Account} instead — balance folded in. */
export interface FinanceAccount {
  id: string;
  name: string;
  kind: AccountKind;
  currency: string;
  note: string | null;
  createdAt: string;
  updatedAt: string;
}

/**
 * An account with what is in it. Nothing here is stored — the balance is the
 * ledger plus what the deposits moved — which is why setting it writes an
 * adjustment entry rather than a number.
 */
export interface Account {
  id: string;
  name: string;
  kind: AccountKind;
  currency: string;
  note: string | null;
  /** In the account's own currency: what the bank app would say. */
  balance: number;
  balanceBase: number;
  /** Market value of the positions held here. Zero for a bank account. */
  holdingsBase: number;
  valueBase: number;
  /** On its way back from a deposit — not in the balance yet. */
  maturingBase: number;
  nextMaturityOn: string | null;
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
  accountId: string;
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
  accountId: string;
  symbol: string;
  name: string | null;
  currency: string;
  lastPrice: number | null;
  lastPriceAt: string | null;
  createdAt: string;
}

export interface Position {
  id: string;
  accountId: string;
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
  /** The principal leaves this account while the deposit runs. Null predates accounts. */
  sourceAccountId: string | null;
  /** Where principal plus interest lands once it matures. Defaults to the source. */
  targetAccountId: string | null;
  note: string | null;
  createdAt: string;
  updatedAt: string;
}

/** A lump off the principal, over and above the monthly payment. */
export interface DebtPayment {
  id: string;
  debtId: string;
  amount: number;
  paidOn: string;
  /** Named, the cash leaves it on paidOn. Null means you logged it yourself. */
  accountId: string | null;
  note: string | null;
  createdAt: string;
}

/**
 * A debt with its schedule run. Everything from `outstanding` down is amortised
 * on the server, never stored, so deleting a chunk moves the payoff date back
 * out on its own.
 */
export interface Debt {
  id: string;
  name: string;
  /** What was borrowed at origination, not what is left. */
  principal: number;
  currency: string;
  /** A percentage as the lender writes it: 3.25 means 3.25%. */
  annualRate: number;
  /** Monthly. */
  payment: number;
  startsOn: string;
  /** The contract's last payment. Compare with paidOffOn. */
  endsOn: string | null;
  assetValue: number | null;
  downPayment: number | null;
  downPaymentAccountId: string | null;
  disbursesToAccountId: string | null;
  note: string | null;

  outstanding: number;
  outstandingBase: number;
  assetValueBase: number | null;
  paymentBase: number;
  /** When the balance actually reaches zero. Null if the payment never clears it. */
  paidOffOn: string | null;
  /** Left standing on endsOn when the payments run out first — a lease residual. */
  balloonBase: number;
  /** Interest still to pay from this month on. What a chunk buys you. */
  interestRemainingBase: number;
  payments: DebtPayment[];
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
  /** What the debts bought, held flat. Only debts that have started. */
  assetsBase: number;
  /** Still owed across every debt, amortised to today. */
  debtsBase: number;
  /** Cash + deposits + holdings + assets − debts. */
  netWorthBase: number;
  monthlyIncomeBase: number;
  /** Includes the payment on every debt still running, flow or no flow. */
  monthlyExpenseBase: number;
  /** The totals are short by these — say so rather than showing a flat number. */
  hasUnpricedHoldings: boolean;
  accounts: Account[];
  flows: FinanceFlow[];
  positions: Position[];
  deposits: Deposit[];
  debts: Debt[];
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
  /** What the debts bought. Appears the month one starts. */
  assets: number | null;
  /** Still owed at month end. Positive — the chart negates it to draw it. */
  debts: number | null;
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
  accountId: string;
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
  accountId: string;
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
  sourceAccountId: string | null;
  targetAccountId: string | null;
  note: string | null;
}

export interface DebtDraft {
  name: string;
  principal: number;
  currency: string;
  annualRate: number;
  payment: number;
  startsOn: string;
  endsOn: string | null;
  assetValue: number | null;
  downPayment: number | null;
  downPaymentAccountId: string | null;
  disbursesToAccountId: string | null;
  note: string | null;
}

export interface DebtPaymentDraft {
  amount: number;
  paidOn: string;
  accountId: string | null;
  note: string | null;
}

export interface AccountDraft {
  name: string;
  kind: AccountKind;
  currency: string;
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

// ── Learning ─────────────────────────────────────────────────────────────────

/** LearningTier: 1 primary … 4 nice to have. Four fixed rungs, not a free sort. */
export type LearningTier = 1 | 2 | 3 | 4;

export const LearningTiers = { Primary: 1, Secondary: 2, Tertiary: 3, NiceToHave: 4 } as const;

export const TIER_LABELS: Record<LearningTier, string> = {
  1: 'Primary',
  2: 'Secondary',
  3: 'Tertiary',
  4: 'Nice to have',
};

/** LearningGoalStatus: 1 active, 2 achieved, 3 parked. */
export type LearningGoalStatus = 1 | 2 | 3;

export const GOAL_STATUS_LABELS: Record<LearningGoalStatus, string> = {
  1: 'Active',
  2: 'Achieved',
  3: 'Parked',
};

/** LearningStepKind: 1 study, 2 certification, 3 project, 4 course, 5 assignment, 6 licence, 7 milestone. */
export type LearningStepKind = 1 | 2 | 3 | 4 | 5 | 6 | 7;

export const StepKinds = {
  Study: 1,
  Certification: 2,
  Project: 3,
  Course: 4,
  Assignment: 5,
  Licence: 6,
  Milestone: 7,
} as const;

export const STEP_KIND_LABELS: Record<LearningStepKind, string> = {
  1: 'Study',
  2: 'Cert',
  3: 'Project',
  4: 'Course',
  5: 'Assignment',
  6: 'Licence',
  7: 'Milestone',
};

/** LearningStepStatus: 1 planned, 2 doing, 3 done, 4 dropped. */
export type LearningStepStatus = 1 | 2 | 3 | 4;

export const StepStatus = { Planned: 1, Doing: 2, Done: 3, Dropped: 4 } as const;

export const STEP_STATUS_LABELS: Record<LearningStepStatus, string> = {
  1: 'Planned',
  2: 'Doing',
  3: 'Done',
  4: 'Dropped',
};

/**
 * CredentialRenewalKind: 1 permanent, 2 expires, 3 unknown.
 *
 * Three states rather than a nullable expiry, because "never expires" and
 * "nobody has checked" need different treatment — an old MCSD should never
 * appear in a renewal list again.
 */
export type CredentialRenewalKind = 1 | 2 | 3;

export const RenewalKinds = { Permanent: 1, Expires: 2, Unknown: 3 } as const;

export const RENEWAL_KIND_LABELS: Record<CredentialRenewalKind, string> = {
  1: 'Permanent',
  2: 'Expires',
  3: 'Not checked',
};

export interface LearningPhase {
  title: string;
  weeks: number;
  focus: string;
  outcome: string;
}

export interface LearningCertificationSuggestion {
  name: string;
  issuer: string;
  code: string;
  order: number;
  typicalCost: string;
  prepHours: number;
  why: string;
  validity: string;
}

/** provider + title is the handle that survives; url is best-effort. */
export interface LearningResource {
  title: string;
  kind: string;
  provider: string;
  url: string;
  cost: string;
  why: string;
  phase: number;
}

export interface LearningProject {
  title: string;
  level: string;
  build: string;
  proves: string;
}

/** The drafted route. Suggested, not committed — steps are what got committed. */
export interface LearningPlan {
  summary: string;
  /** What "prepared" means, concretely. The answer to "what level do I want". */
  targetDefinition: string;
  assumedLevel: string;
  weeklyHours: number;
  prerequisites: string[];
  phases: LearningPhase[];
  certifications: LearningCertificationSuggestion[];
  resources: LearningResource[];
  projects: LearningProject[];
  handsOn: string[];
  risks: string[];
}

export interface LearningStep {
  id: string;
  goalId: string;
  title: string;
  kind: LearningStepKind;
  status: LearningStepStatus;
  targetOn: string | null;
  /** What to do. */
  notes: string;
  /** What happened: the grade, the retake. The row badges when this is set. */
  outcome: string;
  provider: string | null;
  url: string | null;
  cost: string | null;
  hours: number | null;
  sortOrder: number;
  /** The pending task it has on the board, if any. */
  task: TaskListItem | null;
  createdAt: string;
  updatedAt: string;
}

export interface LearningGoal {
  id: string;
  title: string;
  tier: LearningTier;
  status: LearningGoalStatus;
  why: string;
  benefits: string;
  targetOn: string | null;
  /** What the draft was given: current level, hours a week, constraints. */
  context: string;
  /** Null until the first successful draft. */
  plan: LearningPlan | null;
  researchedAt: string | null;
  researchModel: string | null;
  notes: string;
  steps: LearningStep[];
  /** Negative once the target has passed. Derived server-side. */
  daysUntilTarget: number | null;
  createdAt: string;
  updatedAt: string;
}

export interface CredentialRenewal {
  /** Null for permanent, and when the lookup could not establish it. */
  validityMonths: number | null;
  renewal: string;
  windowOpensDays: number;
  cost: string;
  ifLapsed: string;
  officialUrl: string;
  notes: string;
}

/** Named for what it is rather than `Credential`, which the DOM already has. */
export interface HeldCredential {
  id: string;
  goalId: string | null;
  name: string;
  issuer: string;
  code: string | null;
  earnedOn: string;
  renewalKind: CredentialRenewalKind;
  expiresOn: string | null;
  credentialId: string | null;
  url: string | null;
  /** Null until the renewal rules have been looked up. */
  renewal: CredentialRenewal | null;
  researchedAt: string | null;
  researchModel: string | null;
  notes: string;
  /** Null for permanent and for unchecked — never render those as a countdown. */
  daysUntilExpiry: number | null;
  /** Whether the renewal window is open, from the window the lookup found. */
  isRenewable: boolean;
  task: TaskListItem | null;
  createdAt: string;
  updatedAt: string;
}

export interface LearningOverview {
  goals: LearningGoal[];
  credentials: HeldCredential[];
}

export interface GoalDraft {
  title: string;
  tier: LearningTier;
  why: string | null;
  benefits: string | null;
  targetOn: string | null;
  context: string | null;
  notes: string | null;
}

export interface GoalEdit extends GoalDraft {
  status: LearningGoalStatus;
}

/** One line being committed to a path — off the plan, or typed by hand. */
export interface StepInput {
  title: string;
  kind: LearningStepKind;
  targetOn: string | null;
  notes: string | null;
  provider: string | null;
  url: string | null;
  cost: string | null;
  hours: number | null;
}

/** A full replace: the server takes every field, not a patch. */
export interface StepEdit extends StepInput {
  status: LearningStepStatus;
  outcome: string | null;
  sortOrder: number;
}

export interface CredentialDraft {
  name: string;
  issuer: string | null;
  code: string | null;
  earnedOn: string;
  renewalKind: CredentialRenewalKind;
  expiresOn: string | null;
  goalId: string | null;
  credentialId: string | null;
  url: string | null;
  notes: string | null;
}

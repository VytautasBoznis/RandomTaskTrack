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
  /** Set once the dish is on the board. */
  taskId: string | null;
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

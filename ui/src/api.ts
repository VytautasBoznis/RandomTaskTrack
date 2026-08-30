import type {
  AccountDraft,
  CatalogStatus,
  ChatReply,
  CompletionLogItem,
  ConversationDetail,
  ConversationListItem,
  Dashboard,
  Deposit,
  DepositDraft,
  Dividend,
  DividendDraft,
  EntryDraft,
  FinanceAccount,
  FinanceFlow,
  FinanceOverview,
  FinanceTarget,
  FlowDraft,
  Holding,
  HoldingDraft,
  LedgerEntry,
  Note,
  NoteDraft,
  Plant,
  PlantCareTask,
  PlantDraft,
  PlantEdit,
  PlantPhotoDraft,
  PlantSowingStep,
  PriceRefreshResult,
  ProjectionPoint,
  RecipeCandidate,
  RecipeFamily,
  RecipeHistoryItem,
  RecipeMetaDraft,
  Recurrence,
  RecurrenceDraft,
  Session,
  TargetDraft,
  TaskDomain,
  TaskDraft,
  TaskItemStatus,
  TaskListItem,
  Trade,
  TradeDraft,
  WeeklyDish,
} from './types';

// Always relative: in production the ingress routes /api to the API and / here,
// so this is same-origin and CORS never comes into play. In dev the vite proxy
// stands in for the ingress.
const API_BASE = '/api';

const TOKEN_KEY = 'rtt.token';

export const getToken = () => localStorage.getItem(TOKEN_KEY);
export const clearToken = () => localStorage.removeItem(TOKEN_KEY);

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getToken();

  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  });

  if (!response.ok) {
    // ErrorHandlingFilterAttribute returns { message, errorCode, description }.
    const body = await response.json().catch(() => null);
    throw new ApiError(body?.message || `Request failed (${response.status})`, response.status);
  }

  return (await response.json()) as T;
}

export async function login(email: string, password: string): Promise<Session> {
  const { session } = await request<{ session: Session }>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });

  localStorage.setItem(TOKEN_KEY, session.jwtToken);

  return session;
}

// Role is left to the server default (User) — the form never offers it.
export const register = (email: string, password: string) =>
  request<{ userId: string }>('/auth/register', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });

export const getDashboard = async () =>
  (await request<{ dashboard: Dashboard }>('/tasks/dashboard')).dashboard;

export const getDomains = async () => (await request<{ domains: TaskDomain[] }>('/domains')).domains;

export const createTask = (draft: TaskDraft) =>
  request<{ task: TaskListItem }>('/tasks', {
    method: 'POST',
    body: JSON.stringify(draft),
  });

export const updateTask = (id: string, draft: TaskDraft) =>
  request<{ task: TaskListItem }>(`/tasks/${id}`, {
    method: 'PUT',
    body: JSON.stringify(draft),
  });

// No actualData: without domain-specific UI there is nothing to adjust, and the
// server logs the planned payload as the actual one in that case.
export const completeTask = (id: string, status: TaskItemStatus) =>
  request<{ task: TaskListItem }>(`/tasks/${id}/complete`, {
    method: 'POST',
    body: JSON.stringify({ status }),
  });

export const deleteTask = (id: string) =>
  request<{ success: boolean }>(`/tasks/${id}`, { method: 'DELETE' });

export const getCompletionLog = async (domainId: number | null) => {
  const query = domainId === null ? '' : `?domainId=${domainId}`;

  return (await request<{ entries: CompletionLogItem[] }>(`/tasks/completions${query}`)).entries;
};

// Paused recurrences are fetched too — otherwise pausing one would hide it with
// no way to bring it back.
export const getRecurrences = async () =>
  (await request<{ recurrences: Recurrence[] }>('/recurrences?includeInactive=true')).recurrences;

export const createRecurrence = (draft: RecurrenceDraft) =>
  request<{ recurrence: Recurrence; materializedTaskCount: number }>('/recurrences', {
    method: 'POST',
    body: JSON.stringify(draft),
  });

export const updateRecurrence = (id: string, draft: RecurrenceDraft) =>
  request<{ recurrence: Recurrence }>(`/recurrences/${id}`, {
    method: 'PUT',
    body: JSON.stringify(draft),
  });

// Everything the update request leaves null keeps its current value, so this
// pauses or resumes without touching the schedule.
export const setRecurrenceActive = (id: string, isActive: boolean) =>
  request<{ recurrence: Recurrence }>(`/recurrences/${id}`, {
    method: 'PUT',
    body: JSON.stringify({ isActive }),
  });

export const deleteRecurrence = (id: string) =>
  request<{ success: boolean; deletedTaskCount: number }>(`/recurrences/${id}`, { method: 'DELETE' });

// The weekly dish is pulled on first read of the week, so this GET can write.
// `dish` is null when the week has been deliberately cleared.
export const getWeeklyDish = () =>
  request<{ dish: WeeklyDish | null; families: RecipeFamily[] }>('/recipes/weekly');

export const clearWeeklyDish = () =>
  request<{ cleared: boolean }>('/recipes/pick', { method: 'DELETE' });

export const rerollDish = async (familyId: number | null) =>
  (
    await request<{ dish: WeeklyDish }>('/recipes/reroll', {
      method: 'POST',
      body: JSON.stringify({ familyId }),
    })
  ).dish;

export const createDishTask = (pickId: string, dueOn: string | null) =>
  request<{ task: TaskListItem }>('/recipes/task', {
    method: 'POST',
    body: JSON.stringify({ pickId, dueOn }),
  });

export const getCatalogStatus = async () =>
  (await request<{ status: CatalogStatus }>('/recipes/catalog')).status;

// Returns as soon as the run is queued; poll getCatalogStatus for progress.
// Safe to call twice — `started: false` means one was already going.
export const startCatalogImport = () =>
  request<{ started: boolean; status: CatalogStatus }>('/recipes/catalog/import', { method: 'POST' });

// Reads only — nothing is stored until saveRecipes sends the chosen ones back.
// Returns the envelope rather than the list: the catalog answers "ramen" with a
// thousand dishes, so the caller needs hasMore and the page size to step
// through them.
export const searchRecipes = (query: string, offset: number) =>
  request<{ candidates: RecipeCandidate[]; hasMore: boolean; pageSize: number }>(
    `/recipes/search?query=${encodeURIComponent(query)}&offset=${offset}`,
  );

export const saveRecipes = async (recipes: RecipeCandidate[]) =>
  (
    await request<{ recipes: RecipeHistoryItem[] }>('/recipes/library', {
      method: 'POST',
      body: JSON.stringify({ recipes }),
    })
  ).recipes;

export const setWeeklyDish = async (recipeId: string) =>
  (
    await request<{ dish: WeeklyDish }>('/recipes/pick', {
      method: 'POST',
      body: JSON.stringify({ recipeId }),
    })
  ).dish;

export const getRecipeHistory = async (search: string, tags: string[], cooked: boolean | null) => {
  const query = new URLSearchParams();

  if (search !== '') query.set('search', search);
  if (tags.length > 0) query.set('tags', tags.join(','));
  if (cooked !== null) query.set('cooked', String(cooked));

  const suffix = query.toString() === '' ? '' : `?${query}`;

  return (await request<{ entries: RecipeHistoryItem[] }>(`/recipes/history${suffix}`)).entries;
};

export const updateRecipe = async (recipeId: string, draft: RecipeMetaDraft) =>
  (
    await request<{ recipe: RecipeHistoryItem }>(`/recipes/${recipeId}`, {
      method: 'PUT',
      body: JSON.stringify(draft),
    })
  ).recipe;

export const getNotes = async () => (await request<{ notes: Note[] }>('/notes')).notes;

export const createNote = (draft: NoteDraft) =>
  request<{ note: Note }>('/notes', {
    method: 'POST',
    body: JSON.stringify(draft),
  });

export const updateNote = (id: string, draft: NoteDraft) =>
  request<{ note: Note }>(`/notes/${id}`, {
    method: 'PUT',
    body: JSON.stringify(draft),
  });

export const deleteNote = (id: string) =>
  request<{ success: boolean }>(`/notes/${id}`, { method: 'DELETE' });

// ── Plants ───────────────────────────────────────────────────────────────────
// One read for the whole tab: each plant arrives with its care schedule and the
// tasks that schedule has on the board.

export const getPlants = async () => (await request<{ plants: Plant[] }>('/plants')).plants;

/**
 * The plant is saved whether or not the lookup answered — `researchError` says
 * why it did not, and the card offers to try again.
 */
export const createPlant = (draft: PlantDraft) =>
  request<{ plant: Plant; researchError: string | null }>('/plants', {
    method: 'POST',
    body: JSON.stringify(draft),
  });

export const updatePlant = async (id: string, edit: PlantEdit) =>
  (
    await request<{ plant: Plant }>(`/plants/${id}`, {
      method: 'PUT',
      body: JSON.stringify(edit),
    })
  ).plant;

/** Null description re-asks with the stored one — the retry after a failure. */
export const researchPlant = async (id: string, description: string | null, usePhoto: boolean) =>
  (
    await request<{ plant: Plant }>(`/plants/${id}/research`, {
      method: 'POST',
      body: JSON.stringify({ description, usePhoto }),
    })
  ).plant;

/** Adding a photo is how a stage gets recorded; the AI labels it. */
export const addPlantPhoto = (id: string, draft: PlantPhotoDraft) =>
  request<{ plant: Plant; readError: string | null }>(`/plants/${id}/photos`, {
    method: 'POST',
    body: JSON.stringify(draft),
  });

export const deletePlantPhoto = (photoId: string) =>
  request<{ success: boolean }>(`/plants/photos/${photoId}`, { method: 'DELETE' });

/**
 * Photos sit behind the same bearer token as everything else and an <img> tag
 * cannot send a header, so the bytes are fetched like any other call and handed
 * to the tag as an object URL. The caller owns revoking it.
 */
export async function fetchPlantPhoto(photoId: string): Promise<string> {
  const token = getToken();

  const response = await fetch(`${API_BASE}/plants/photos/${photoId}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (!response.ok) {
    throw new ApiError(`Could not load the photo (${response.status})`, response.status);
  }

  return URL.createObjectURL(await response.blob());
}

/** Dates the whole chain from the day it actually gets sown. */
export const createSowingPlan = (id: string, sowOn: string, steps: PlantSowingStep[]) =>
  request<{ plant: Plant; createdTaskCount: number }>(`/plants/${id}/sowing`, {
    method: 'POST',
    body: JSON.stringify({ sowOn, steps }),
  });

export const createPlantSchedule = (id: string, tasks: PlantCareTask[]) =>
  request<{ plant: Plant; materializedTaskCount: number }>(`/plants/${id}/schedule`, {
    method: 'POST',
    body: JSON.stringify({ tasks }),
  });

export const deletePlant = (id: string) =>
  request<{ success: boolean; deletedRecurrenceCount: number; deletedTaskCount: number }>(`/plants/${id}`, {
    method: 'DELETE',
  });

export const getConversations = async () =>
  (await request<{ conversations: ConversationListItem[] }>('/chat/conversations')).conversations;

export const getConversation = async (id: string) =>
  (await request<{ conversation: ConversationDetail }>(`/chat/conversations/${id}`)).conversation;

export const sendChatMessage = (conversationId: string | null, message: string, domainId: number | null) =>
  request<ChatReply>('/chat/messages', {
    method: 'POST',
    body: JSON.stringify({ conversationId, message, domainId }),
  });

export const deleteConversation = (id: string) =>
  request<{ success: boolean }>(`/chat/conversations/${id}`, { method: 'DELETE' });

// ── Finance ──────────────────────────────────────────────────────────────────
// The overview is one round trip for the whole tab, the same bargain
// /tasks/dashboard makes. Everything else is plain CRUD.

export const getFinanceOverview = async () =>
  (await request<{ overview: FinanceOverview }>('/finance/overview')).overview;

/**
 * `stockGrowth` is an annual percentage. Zero holds the portfolio at its last
 * pulled price, which is the honest default — the caller chooses the optimism.
 */
export const getProjection = async (months: number, historyMonths: number, stockGrowth: number) =>
  (
    await request<{ points: ProjectionPoint[] }>(
      `/finance/projection?months=${months}&historyMonths=${historyMonths}&stockGrowth=${stockGrowth}`,
    )
  ).points;

// Safe to press twice. A symbol with no price is reported in `failed` and keeps
// whatever price it had.
export const refreshPrices = () =>
  request<PriceRefreshResult>('/finance/prices/refresh', { method: 'POST' });

// Accounts. There is no endpoint that writes a balance: setBalance types the
// number you can see and the server logs the difference as an entry, so the
// total stays something the ledger explains.
export const createAccount = (draft: AccountDraft) =>
  request<{ account: FinanceAccount }>('/finance/accounts', {
    method: 'POST',
    body: JSON.stringify(draft),
  });

export const updateAccount = (id: string, draft: Partial<AccountDraft>) =>
  request<{ account: FinanceAccount }>(`/finance/accounts/${id}`, {
    method: 'PUT',
    body: JSON.stringify(draft),
  });

export const deleteAccount = (id: string) =>
  request<{ success: boolean }>(`/finance/accounts/${id}`, { method: 'DELETE' });

/** `entry` comes back null when the balance already read what was asked for. */
export const setAccountBalance = (id: string, balance: number, note: string | null) =>
  request<{ entry: LedgerEntry | null }>(`/finance/accounts/${id}/balance`, {
    method: 'POST',
    body: JSON.stringify({ balance, note }),
  });

export const createFlow = (draft: FlowDraft) =>
  request<{ flow: FinanceFlow }>('/finance/flows', { method: 'POST', body: JSON.stringify(draft) });

export const updateFlow = (id: string, draft: Partial<FlowDraft>) =>
  request<{ flow: FinanceFlow }>(`/finance/flows/${id}`, { method: 'PUT', body: JSON.stringify(draft) });

// Everything left out keeps its value, so this pauses without touching the schedule.
export const setFlowActive = (id: string, isActive: boolean) =>
  request<{ flow: FinanceFlow }>(`/finance/flows/${id}`, {
    method: 'PUT',
    body: JSON.stringify({ isActive }),
  });

export const deleteFlow = (id: string) =>
  request<{ success: boolean }>(`/finance/flows/${id}`, { method: 'DELETE' });

export const getEntries = async (from: string | null, to: string | null, search: string) => {
  const query = new URLSearchParams();

  if (from) query.set('from', from);
  if (to) query.set('to', to);
  if (search !== '') query.set('search', search);

  const suffix = query.toString() === '' ? '' : `?${query}`;

  return (await request<{ entries: LedgerEntry[] }>(`/finance/entries${suffix}`)).entries;
};

export const createEntry = (draft: EntryDraft) =>
  request<{ entry: LedgerEntry }>('/finance/entries', { method: 'POST', body: JSON.stringify(draft) });

export const updateEntry = (id: string, draft: Partial<EntryDraft>) =>
  request<{ entry: LedgerEntry }>(`/finance/entries/${id}`, { method: 'PUT', body: JSON.stringify(draft) });

export const deleteEntry = (id: string) =>
  request<{ success: boolean }>(`/finance/entries/${id}`, { method: 'DELETE' });

export const createHolding = (draft: HoldingDraft) =>
  request<{ holding: Holding }>('/finance/holdings', { method: 'POST', body: JSON.stringify(draft) });

export const deleteHolding = (id: string) =>
  request<{ success: boolean }>(`/finance/holdings/${id}`, { method: 'DELETE' });

export const createTrade = (draft: TradeDraft) =>
  request<{ trade: Trade }>('/finance/trades', { method: 'POST', body: JSON.stringify(draft) });

export const updateTrade = (id: string, draft: Partial<TradeDraft>) =>
  request<{ trade: Trade }>(`/finance/trades/${id}`, { method: 'PUT', body: JSON.stringify(draft) });

export const deleteTrade = (id: string) =>
  request<{ success: boolean }>(`/finance/trades/${id}`, { method: 'DELETE' });

export const createDividend = (draft: DividendDraft) =>
  request<{ dividend: Dividend }>('/finance/dividends', { method: 'POST', body: JSON.stringify(draft) });

export const setDividendActive = (id: string, isActive: boolean) =>
  request<{ dividend: Dividend }>(`/finance/dividends/${id}`, {
    method: 'PUT',
    body: JSON.stringify({ isActive }),
  });

export const deleteDividend = (id: string) =>
  request<{ success: boolean }>(`/finance/dividends/${id}`, { method: 'DELETE' });

export const createDeposit = (draft: DepositDraft) =>
  request<{ deposit: Deposit }>('/finance/deposits', { method: 'POST', body: JSON.stringify(draft) });

export const updateDeposit = (id: string, draft: Partial<DepositDraft>) =>
  request<{ deposit: Deposit }>(`/finance/deposits/${id}`, { method: 'PUT', body: JSON.stringify(draft) });

export const deleteDeposit = (id: string) =>
  request<{ success: boolean }>(`/finance/deposits/${id}`, { method: 'DELETE' });

export const createTarget = (draft: TargetDraft) =>
  request<{ target: FinanceTarget }>('/finance/targets', { method: 'POST', body: JSON.stringify(draft) });

export const deleteTarget = (id: string) =>
  request<{ success: boolean }>(`/finance/targets/${id}`, { method: 'DELETE' });

// Mirrors the DTOs in RandomTaskTrack.Data. ASP.NET serialises PascalCase
// properties as camelCase, and DateOnly/TimeOnly as "yyyy-MM-dd" / "HH:mm:ss".

/** TaskItemStatus: 1 Pending, 2 Done, 3 Skipped. */
export type TaskItemStatus = 1 | 2 | 3;

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

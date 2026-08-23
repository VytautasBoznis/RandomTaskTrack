import { useCallback, useEffect, useState } from 'react';
import { ApiError, getDomains } from './api';
import type { TaskDomain } from './types';

/**
 * Every view needs the same two things from a failed call: sign out on 401,
 * show the message otherwise.
 */
export function useApiError(onUnauthorized: () => void) {
  const [error, setError] = useState<string | null>(null);

  const fail = useCallback(
    (e: unknown) => {
      // A 30-day token outlives most things, but not a rotated Jwt:SecretKey.
      if (e instanceof ApiError && e.status === 401) {
        onUnauthorized();
        return;
      }

      setError(e instanceof Error ? e.message : 'Something went wrong');
    },
    [onUnauthorized],
  );

  return { error, setError, fail };
}

export function useDomains(fail: (e: unknown) => void) {
  const [domains, setDomains] = useState<TaskDomain[]>([]);

  useEffect(() => {
    getDomains().then(setDomains).catch(fail);
  }, [fail]);

  return domains;
}

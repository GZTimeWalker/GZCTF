import { useMemo } from 'react';
import { useLocation } from 'react-router-dom';

export function useQueryParams(): Record<string, string | null> {
  const location = useLocation();
  const proxy = useMemo(() => {
    const params = new URLSearchParams(decodeURIComponent(location.search.substring(1)));
    return new Proxy(
      {},
      {
        get(_, property) {
          if (typeof property === 'symbol') {
            return null;
          }
          return params.get(property) ?? null;
        }
      }
    );
  }, [location.search]);
  return proxy;
}

const fetcher = async (arg: string | [string, Record<string, string>]) => {
  let res: Response;

  if (typeof arg === 'string') {
    res = await fetch(arg);
  } else {
    const qs = new URLSearchParams(arg[1]).toString();
    res = await fetch(`${arg[0]}?${qs}`);
  }

  if (!res.ok) throw res;

  return res.headers.get('content-type')?.includes('application/json')
    ? await res.json()
    : await res.text();
};

export default fetcher;

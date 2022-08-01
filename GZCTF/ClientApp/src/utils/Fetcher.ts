import axios from 'axios'

export const fetcher = async (arg: string | [string, Record<string, string>]) => {
  const [url, params] = typeof arg === 'string' ? [arg, {}] : arg
  return await axios
    .get(url, { params })
    .then((res) => res.data)
    .catch((err) => {
      throw err.response.data
    })
}

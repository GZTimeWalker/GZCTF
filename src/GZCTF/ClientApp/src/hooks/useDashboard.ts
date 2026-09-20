import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import useSWR from 'swr'
import { fetcher } from '@Api'
import { useEffect, useState } from 'react'

export type DashboardPoint = { date: string; value: number }
export type DashboardMember = { userName: string; cohortId?: string; points: DashboardPoint[]; uniqueSolvedCount: number }
export type DashboardSnapshot = { dashboardId: string; generatedAtUtc: string; cohorts: { id: string; name: string }[]; members: DashboardMember[]; leaderboard: { userName: string; uniqueSolvedCount: number; rank: number }[] }

export const useDashboard = (id: string | undefined, token: string | undefined, cohortId?: string, search?: string) => {
  const query = new URLSearchParams({ token: token ?? '' })
  if (cohortId) query.set('cohortId', cohortId)
  if (search) query.set('search', search)
  const key = id && token ? `/api/dashboards/${id}?${query}` : null
  const { data, error, mutate } = useSWR<DashboardSnapshot>(key, fetcher)
  const [connected, setConnected] = useState(false)

  useEffect(() => {
    if (!id || !token) return
    const connection = new HubConnectionBuilder()
      .withUrl(`/hub/dashboard?id=${encodeURIComponent(id)}&token=${encodeURIComponent(token)}`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()
    connection.on('solveDelta', () => { void mutate() })
    connection.onreconnecting(() => setConnected(false))
    connection.onreconnected(() => { setConnected(true); void mutate() })
    void connection.start().then(() => setConnected(true)).catch(() => setConnected(false))
    return () => { setConnected(false); void connection.stop() }
  }, [id, token, mutate])

  return { data, error, connected }
}


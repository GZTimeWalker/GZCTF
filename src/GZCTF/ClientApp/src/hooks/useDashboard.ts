import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import useSWR from 'swr'
import { DashboardMemberSeries, DashboardPoint as DashboardPointResponse, DashboardSnapshotResponse, fetcher } from '@Api'
import { useEffect, useMemo, useRef, useState } from 'react'

export type DashboardPoint = Required<Pick<DashboardPointResponse, 'date' | 'value'>>
export type DashboardMember = Required<Pick<DashboardMemberSeries, 'userName' | 'points' | 'uniqueSolvedCount'>> & { cohortId?: string }
export type DashboardSnapshot = { dashboardId: string; generatedAtUtc: string; cohorts: { id: string; name: string }[]; members: DashboardMember[]; leaderboard: { userName: string; uniqueSolvedCount: number; rank: number }[] }

export const useDashboard = (id: string | undefined, token: string | undefined, cohortId?: string, search?: string) => {
  const query = new URLSearchParams({ token: token ?? '' })
  if (cohortId) query.set('cohortId', cohortId)
  if (search) query.set('search', search)
  const key = id && token ? `/api/dashboards/${id}?${query}` : null
  const { data: response, error, mutate } = useSWR<DashboardSnapshotResponse>(key, fetcher)
  const [connected, setConnected] = useState(false)
  const mutateRef = useRef(mutate)

  useEffect(() => { mutateRef.current = mutate }, [mutate])

  useEffect(() => {
    if (!id || !token) return
    const connection = new HubConnectionBuilder()
      .withUrl(`/hub/dashboard?id=${encodeURIComponent(id)}&token=${encodeURIComponent(token)}`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()
    connection.on('solveDelta', () => { void mutateRef.current() })
    connection.onreconnecting(() => setConnected(false))
    connection.onreconnected(() => { setConnected(true); void mutateRef.current() })
    void connection.start().then(() => setConnected(true)).catch(() => setConnected(false))
    return () => { setConnected(false); void connection.stop() }
  }, [id, token])

  const data = useMemo<DashboardSnapshot | undefined>(() => response ? ({
    dashboardId: response.dashboardId ?? '',
    generatedAtUtc: response.generatedAtUtc === undefined ? '' : String(response.generatedAtUtc),
    cohorts: (response.cohorts ?? []).map(item => ({ id: item.id ?? '', name: item.name ?? '' })),
    members: (response.members ?? []).map(member => ({
      userName: member.userName ?? '',
      cohortId: member.cohortId ?? undefined,
      uniqueSolvedCount: member.uniqueSolvedCount ?? 0,
      points: (member.points ?? []).map(point => ({ date: point.date ?? '', value: point.value ?? 0 })),
    })),
    leaderboard: (response.leaderboard ?? []).map(item => ({
      userName: item.userName ?? '',
      uniqueSolvedCount: item.uniqueSolvedCount ?? 0,
      rank: item.rank ?? 0,
    })),
  }) : undefined, [response])

  return { data, error, connected }
}

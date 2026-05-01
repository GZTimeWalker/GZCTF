import { useMantineColorScheme, useMantineTheme } from '@mantine/core'
import type { EChartsOption } from 'echarts'
import { FC, useMemo } from 'react'
import { useParams } from 'react-router'
import { EchartsContainer } from '@Components/charts/EchartsContainer'
import { useGameScoreboard } from '@Hooks/useGame'

interface ScoreHeatMapProps {
  divisionId: number | null
}

export const ScoreHeatMap: FC<ScoreHeatMapProps> = ({ divisionId }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()
  const { scoreboard } = useGameScoreboard(numId)

  const { teams, challengeList, heatData } = useMemo(() => {
    if (!scoreboard) return { teams: [], challengeList: [], heatData: [] }

    const selectedDiv = divisionId === null ? 0 : divisionId

    // Filter items by division
    const items = divisionId === null
      ? scoreboard.items
      : scoreboard.items.filter((t) => t.divisionId === divisionId)

    // Top 30 teams by rank
    const topTeams = [...items].sort((a, b) => a.rank - b.rank).slice(0, 30)
    const teamNames = topTeams.map((t) => t.name)

    // Build ordered challenge list from scoreboard.challenges
    const challengeList: { id: number; title: string; category: string }[] = []
    if (scoreboard.challenges) {
      for (const [cat, challs] of Object.entries(scoreboard.challenges)) {
        for (const c of challs) {
          challengeList.push({ id: c.id, title: c.title ?? `#${c.id}`, category: cat })
        }
      }
    }

    // Build solve time lookup: teamIndex × challengeIndex → time (ms)
    const teamIndexMap = new Map(topTeams.map((t, i) => [t.id, i]))

    // Earliest solve time per challenge across all teams (for normalization)
    const earliestPerChallenge = new Map<number, number>()
    const latestPerChallenge = new Map<number, number>()
    for (const team of topTeams) {
      for (const solve of team.solvedChallenges ?? []) {
        const t = solve.time
        if (!earliestPerChallenge.has(solve.id) || t < earliestPerChallenge.get(solve.id)!) {
          earliestPerChallenge.set(solve.id, t)
        }
        if (!latestPerChallenge.has(solve.id) || t > latestPerChallenge.get(solve.id)!) {
          latestPerChallenge.set(solve.id, t)
        }
      }
    }

    // heatData: [challengeIndex, teamIndex, normalised 0-1 (0=first, 1=last)]
    const heatData: [number, number, number][] = []
    for (const team of topTeams) {
      const ti = teamIndexMap.get(team.id)!
      for (const solve of team.solvedChallenges ?? []) {
        const ci = challengeList.findIndex((c) => c.id === solve.id)
        if (ci === -1) continue
        const earliest = earliestPerChallenge.get(solve.id) ?? solve.time
        const latest = latestPerChallenge.get(solve.id) ?? solve.time
        const norm = latest === earliest ? 0 : (solve.time - earliest) / (latest - earliest)
        heatData.push([ci, ti, parseFloat(norm.toFixed(3))])
      }
    }

    return { teams: teamNames, challengeList, heatData }
  }, [scoreboard, divisionId])

  const option = useMemo((): EChartsOption => {
    const isDark = colorScheme === 'dark'
    const solvedColor = theme.colors.teal[6]
    const lateColor = theme.colors.orange[6]

    return {
      backgroundColor: 'transparent',
      tooltip: {
        position: 'top',
        formatter: (params: any) => {
          const [ci, ti] = params.data
          const chall = challengeList[ci]
          const team = teams[ti]
          const pct = Math.round(params.data[2] * 100)
          return `${team}<br/>${chall?.title ?? ''}<br/>Relative time: ${pct === 0 ? '🩸 First!' : `+${pct}%`}`
        },
      },
      grid: { left: 120, right: 40, bottom: 100, top: 20 },
      xAxis: {
        type: 'category',
        data: challengeList.map((c) => c.title),
        splitArea: { show: true },
        axisLabel: { rotate: 45, fontSize: 10, color: isDark ? '#aaa' : '#555' },
      },
      yAxis: {
        type: 'category',
        data: teams,
        splitArea: { show: true },
        axisLabel: { fontSize: 11, color: isDark ? '#ccc' : '#333' },
      },
      visualMap: {
        min: 0,
        max: 1,
        calculable: false,
        show: false,
        inRange: { color: [solvedColor, lateColor] },
      },
      series: [{
        type: 'heatmap',
        data: heatData,
        label: { show: false },
        emphasis: { itemStyle: { shadowBlur: 10, shadowColor: 'rgba(0,0,0,0.5)' } },
      }],
    }
  }, [teams, challengeList, heatData, colorScheme, theme])

  if (!scoreboard || challengeList.length === 0 || teams.length === 0) return null

  const height = Math.max(200, teams.length * 22 + 120)

  return (
    <EchartsContainer
      option={option}
      style={{ height, width: '100%' }}
    />
  )
}

import { useMemo } from 'react'
import type { EChartsOption, SeriesOption } from 'echarts'
import { EchartsContainer } from '@Components/charts/EchartsContainer'
import type { DashboardMember } from '@Hooks/useDashboard'

export const SolveTrendChart = ({ members, highlighted }: { members: DashboardMember[]; highlighted?: string }) => {
  const option = useMemo<EChartsOption>(() => ({
    tooltip: { trigger: 'axis' },
    legend: { type: 'scroll' },
    xAxis: { type: 'time' },
    yAxis: { type: 'value', min: 0 },
    series: members.map((member) => ({
      type: 'line', name: member.userName, showSymbol: false,
      lineStyle: { opacity: highlighted && highlighted !== member.userName ? 0.18 : 1, width: highlighted === member.userName ? 3 : 1 },
      data: member.points.map((point) => [point.date, point.value]),
    }) satisfies SeriesOption),
  }), [members, highlighted])
  return <EchartsContainer option={option} style={{ height: 480, width: '100%' }} />
}


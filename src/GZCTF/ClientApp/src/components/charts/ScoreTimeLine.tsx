import { useMantineColorScheme, useMantineTheme } from '@mantine/core'
import dayjs from 'dayjs'
import type { EChartsOption } from 'echarts'
import ReactEcharts from 'echarts-for-react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { normalizeLanguage, useLanguage } from '@Utils/I18n'
import { getGameStatus, useGame, useGameScoreboard } from '@Hooks/useGame'

interface TimeLineProps {
  division: string | null
}

export const ScoreTimeLine: FC<TimeLineProps> = ({ division }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const theme = useMantineTheme()

  const { scoreboard } = useGameScoreboard(numId)

  const { game } = useGame(numId)

  const { startTime, endTime, progress, finished } = getGameStatus(game)

  const totDuration = endTime.diff(startTime, 'd')
  const longGame = totDuration > 14

  const weekProgress = (7 / totDuration) * 100
  const weekStart = progress - weekProgress
  const weekEnd = progress

  const drawStart = longGame && !finished ? weekStart : 0
  const drawEnd = longGame && !finished ? weekEnd : 100

  const [now, setNow] = useState<Date>(new Date())
  const [chartData, setChartData] = useState<any>()

  const { colorScheme } = useMantineColorScheme()
  const { t } = useTranslation()
  const { language } = useLanguage()
  const locale = normalizeLanguage(language)

  useEffect(() => {
    if (!scoreboard?.timeLines || !game) return

    const timeLine = scoreboard?.timeLines[division ?? 'all'] ?? []
    const current = dayjs()
    const last = endTime.diff(current, 's') < 0 ? endTime : current

    setChartData([
      {
        type: 'line',
        step: 'end',
        data: [],
        markLine:
          dayjs(game.end).diff(dayjs(), 's') < 0
            ? undefined
            : {
                symbol: 'none',
                data: [
                  {
                    xAxis: last.toDate(),
                    lineStyle: {
                      color: colorScheme === 'dark' ? theme.colors.gray[3] : theme.colors.gray[6],
                      wight: 2,
                    },
                    label: {
                      textBorderWidth: 0,
                      fontWeight: 500,
                      formatter: (time: any) => dayjs(time.value).format('YYYY-MM-DD HH:mm'),
                    },
                  },
                ],
              },
      },
      ...(timeLine?.map((team) => ({
        type: 'line',
        step: 'end',
        name: team.name,
        data: [
          [dayjs(game.start).toDate(), 0],
          ...(team.items?.map((item) => [item.time, item.score]) ?? []),
          [last.toDate(), (team.items && team.items[team.items.length - 1]?.score) ?? 0],
        ],
      })) ?? []),
    ])

    setNow(new Date())
  }, [scoreboard, division, game])

  const labelColor = colorScheme === 'dark' ? theme.colors.light[1] : theme.colors.dark[5]
  const lineColor = colorScheme === 'dark' ? theme.colors.gray[3] : theme.colors.gray[6]
  const backgroundColor = colorScheme === 'dark' ? theme.colors.gray[6] : theme.colors.light[1]

  const option: EChartsOption = {
    backgroundColor: 'transparent',
    toolbox: {
      show: true,
      feature: {
        dataZoom: {},
        restore: {},
        saveAsImage: {},
      },
    },
    xAxis: {
      type: 'time',
      name: t('common.label.time'),
      min: dayjs(game?.start).toDate(),
      max: dayjs(game?.end).toDate(),
      splitLine: {
        show: false,
      },
    },
    yAxis: {
      type: 'value',
      name: t('game.label.score'),
      boundaryGap: [0, '100%'],
      axisLabel: {
        formatter: t('game.label.score_formatter'),
        color: labelColor,
      },
      max: (value: any) => (Math.floor(value.max / 1000) + 1) * 1000,
      splitLine: {
        show: true,
        lineStyle: {
          color: [lineColor],
          type: 'dashed',
        },
      },
    },
    tooltip: {
      trigger: 'axis',
      borderWidth: 0,
      textStyle: {
        fontSize: 10,
        color: labelColor,
      },
      backgroundColor: backgroundColor,
    },
    legend: {
      orient: 'horizontal',
      bottom: 50,
      textStyle: {
        fontSize: 12,
        color: labelColor,
      },
    },
    grid: {
      top: 50,
      left: 70,
      right: 90,
      bottom: 100,
    },
    dataZoom: [
      {
        type: 'inside',
        start: drawStart,
        end: drawEnd,
        xAxisIndex: 0,
        filterMode: 'none',
      },
      {
        start: drawStart,
        end: drawEnd,
        xAxisIndex: 0,
        showDetail: false,
      },
      {
        type: 'inside',
        start: 0,
        end: 100,
        yAxisIndex: 0,
        filterMode: 'none',
      },
      {
        start: 0,
        end: 100,
        yAxisIndex: 0,
        showDetail: false,
      },
    ],

    series: chartData,
  }

  return (
    <ReactEcharts
      key={now.toUTCString()}
      theme={colorScheme}
      option={option}
      opts={{
        renderer: 'svg',
        locale,
      }}
      style={{
        width: '100%',
        height: '450px',
        display: 'flex',
      }}
    />
  )
}

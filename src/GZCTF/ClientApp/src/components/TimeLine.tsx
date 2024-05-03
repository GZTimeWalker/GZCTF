import { useMantineColorScheme, useMantineTheme } from '@mantine/core'
import dayjs from 'dayjs'
import ReactEcharts from 'echarts-for-react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router-dom'
import { normalizeLanguage, useLanguage } from '@Utils/I18n'
import { getGameStatus, useGame, useGameScoreboard } from '@Utils/useGame'

interface TimeLineProps {
  organization: string | null
}

const TimeLine: FC<TimeLineProps> = ({ organization }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const theme = useMantineTheme()

  const { scoreboard } = useGameScoreboard(numId)

  const { game } = useGame(numId)

  const { startTime, endTime, progress } = getGameStatus(game)

  const totDuration = endTime.diff(startTime, 'd')
  const longGame = totDuration > 14
  const weekProgress = (7 / totDuration) * 100

  const scaleStart = progress - weekProgress
  const scaleEnd = progress

  const [now, setNow] = useState<Date>(new Date())
  const [chartData, setChartData] = useState<any>()

  const { colorScheme } = useMantineColorScheme()
  const { t } = useTranslation()
  const { language } = useLanguage()
  const locale = normalizeLanguage(language)

  useEffect(() => {
    if (!scoreboard?.timeLines || !game) return

    const timeLine = scoreboard?.timeLines[organization ?? 'all'] ?? []
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
  }, [scoreboard, organization, game])

  return (
    <ReactEcharts
      key={now.toUTCString()}
      theme={colorScheme}
      option={{
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
            color: colorScheme === 'dark' ? theme.colors.light[1] : theme.colors.dark[5],
          },
          max: (value: any) => (Math.floor(value.max / 1000) + 1) * 1000,
          splitLine: {
            show: true,
            lineStyle: {
              color: [colorScheme === 'dark' ? theme.colors.gray[5] : theme.colors.gray[3]],
              type: 'dashed',
            },
          },
        },
        tooltip: {
          trigger: 'axis',
          borderWidth: 0,
          textStyle: {
            fontSize: 10,
            color: colorScheme === 'dark' ? theme.colors.light[1] : theme.colors.dark[5],
          },
          backgroundColor: colorScheme === 'dark' ? theme.colors.gray[6] : theme.colors.light[1],
        },
        legend: {
          orient: 'horizontal',
          bottom: 50,
          textStyle: {
            fontSize: 12,
            color: colorScheme === 'dark' ? theme.colors.light[1] : theme.colors.dark[5],
          },
        },
        grid: {
          x: 70,
          y: 50,
          y2: 100,
          x2: 90,
        },
        dataZoom: [
          {
            type: 'inside',
            start: longGame ? scaleStart : 0,
            end: longGame ? scaleEnd : 100,
            xAxisIndex: 0,
            filterMode: 'none',
          },
          {
            start: longGame ? scaleStart : 0,
            end: longGame ? scaleEnd : 100,
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
      }}
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

export default TimeLine

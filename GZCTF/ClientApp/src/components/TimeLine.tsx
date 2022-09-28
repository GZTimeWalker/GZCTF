import dayjs from 'dayjs'
import ReactEcharts from 'echarts-for-react'
import { FC, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { useMantineTheme } from '@mantine/core'
import api from '@Api'

interface TimeLineProps {
  organization: string | null
}

const TimeLine: FC<TimeLineProps> = ({ organization }) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const theme = useMantineTheme()

  const { data: scoreboard } = api.game.useGameScoreboard(numId, {
    refreshInterval: 0,
  })

  const { data: game } = api.game.useGameGames(numId, {
    refreshInterval: 0,
    revalidateOnFocus: false,
  })

  const [now, setNow] = useState<Date>(new Date())
  const [chartData, setChartData] = useState<any>()

  useEffect(() => {
    if (!scoreboard?.timeLines || !game) return

    const timeLine = scoreboard?.timeLines[organization ?? 'all'] ?? []
    const endTime = dayjs(game.end)
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
                      color:
                        theme.colorScheme === 'dark' ? theme.colors.gray[3] : theme.colors.gray[6],
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
      theme={theme.colorScheme}
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
          name: '时间',
          min: dayjs(game?.start).toDate(),
          max: dayjs(game?.end).toDate(),
          splitLine: {
            show: false,
          },
        },
        yAxis: {
          type: 'value',
          name: '分数',
          boundaryGap: [0, '100%'],
          axisLabel: {
            formatter: '{value} 分',
            color: theme.colorScheme === 'dark' ? theme.colors.white[1] : theme.colors.dark[5],
          },
          max: (value: any) => (Math.floor(value.max / 1000) + 1) * 1000,
          splitLine: {
            show: true,
            lineStyle: {
              color: [theme.colorScheme === 'dark' ? theme.colors.gray[5] : theme.colors.gray[3]],
              type: 'dashed',
            },
          },
        },
        tooltip: {
          trigger: 'axis',
          borderWidth: 0,
          textStyle: {
            fontSize: 10,
            color: theme.colorScheme === 'dark' ? theme.colors.white[1] : theme.colors.dark[5],
          },
          backgroundColor:
            theme.colorScheme === 'dark' ? theme.colors.gray[6] : theme.colors.white[1],
        },
        legend: {
          orient: 'horizontal',
          bottom: 50,
          textStyle: {
            fontSize: 12,
            color: theme.colorScheme === 'dark' ? theme.colors.white[1] : theme.colors.dark[5],
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
            start: 0,
            end: 100,
            xAxisIndex: 0,
            filterMode: 'none',
          },
          {
            start: 0,
            end: 100,
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

import ReactEcharts from 'echarts-for-react'
import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { useMantineTheme } from '@mantine/core'
import api from '@Api/Api'

const TimeLine: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const theme = useMantineTheme()

  const { data: scoreboard } = api.game.useGameScoreboard(numId, {
    refreshInterval: 0,
  })

  const chartData = scoreboard?.timeLine?.map((team) => ({
    type: 'line',
    step: 'end',
    name: team.name,
    data: team.items?.map((item) => [item.time, item.score]) ?? [],
  }))

  return (
    <ReactEcharts
      theme={theme.colorScheme}
      option={{
        backgroundColor: 'transparent',
        xAxis: {
          type: 'time',
          name: '时间',
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
          },
          max: (value: any) => (Math.floor(value.max / 1000) + 1) * 1000,
          splitLine: {
            show: true,
            lineStyle: {
              color: [theme.colors.gray[5]],
              type: 'dashed',
            },
          },
        },
        tooltip: {
          trigger: 'axis',
          textStyle: {
            fontSize: 10,
            color: theme.colors.white[1],
          },
          backgroundColor: theme.colors.gray[6],
        },
        legend: {
          orient: 'horizontal',
          bottom: 50,
          textStyle: {
            fontSize: 10,
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
        height: '400px',
        display: 'flex',
      }}
    />
  )
}

export default TimeLine

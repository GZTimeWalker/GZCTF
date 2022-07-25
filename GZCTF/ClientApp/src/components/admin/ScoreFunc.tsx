import ReactEcharts from 'echarts-for-react'
import { FC } from 'react'
import { useMantineTheme } from '@mantine/core'

interface ScoreFuncProps {
  originalScore: number
  difficulty: number
  minScoreRate: number
  currentAcceptCount: number
}

const ScoreFunc: FC<ScoreFuncProps> = ({
  originalScore,
  difficulty,
  minScoreRate,
  currentAcceptCount,
}) => {
  const toX = (x: number) => (x * 6 * difficulty) / 100
  const func = (x: number) =>
    Math.floor(originalScore * (minScoreRate + (1 - minScoreRate) * Math.exp(-x / difficulty)))

  const curScore = func(currentAcceptCount)
  const theme = useMantineTheme()
  const plotData = [...Array(100).keys()].map((x) => [toX(x), func(toX(x))])

  return (
    <ReactEcharts
      theme={theme.colorScheme}
      option={{
        animation: false,
        backgroundColor: 'transparent',
        grid: {
          top: 40,
          left: 40,
          right: 70,
          bottom: 30,
          backgroundColor: 'transparent',
        },
        xAxis: {
          name: '解出次数',
        },
        yAxis: {
          name: '题目分值',
          min: 0,
          max: originalScore,
        },
        series: [
          {
            type: 'line',
            showSymbol: false,
            clip: true,
            color: theme.colors.brand[7],
            data: plotData,
            markPoint: {
              data: [{
                name: '当前分值',
                value: curScore,
                xAxis: currentAcceptCount,
                yAxis: curScore,
              }]
            },
            markLine: {
              symbol: 'none',
              data: [
                {
                  yAxis: Math.floor(originalScore * minScoreRate),
                  label: {
                    position: 'end',
                    formatter: 'min: {c}',
                  },
                },
              ],
            },
          },
        ],
      }}
      opts={{
        renderer: 'svg',
      }}
      style={{
        width: '100%',
        height: '100%',
        display: 'flex',
      }}
    />
  )
}

export default ScoreFunc

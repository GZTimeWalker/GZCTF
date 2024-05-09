import { useMantineColorScheme, useMantineTheme } from '@mantine/core'
import ReactEcharts from 'echarts-for-react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'

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
    x <= 1
      ? originalScore
      : Math.floor(
          originalScore * (minScoreRate + (1 - minScoreRate) * Math.exp((1 - x) / difficulty))
        )

  const curScore = func(currentAcceptCount)
  const showCount = currentAcceptCount > 5.8 * difficulty ? 5.8 * difficulty : currentAcceptCount
  const theme = useMantineTheme()
  const plotData = [...Array(100).keys()].map((x) => [toX(x), func(toX(x))])
  const { colorScheme } = useMantineColorScheme()
  const { t } = useTranslation()

  return (
    <ReactEcharts
      theme={colorScheme}
      option={{
        animation: false,
        backgroundColor: 'transparent',
        grid: {
          top: 30,
          left: 40,
          right: 70,
          bottom: 30,
          backgroundColor: 'transparent',
        },
        xAxis: {
          name: t('admin.content.games.challenges.solve_count'),
        },
        yAxis: {
          name: t('admin.content.games.challenges.score'),
          min: 0,
          max: Math.ceil((originalScore * 1.2) / 100) * 100,
        },
        series: [
          {
            type: 'line',
            showSymbol: false,
            clip: true,
            color: theme.colors[theme.primaryColor][8],
            data: plotData,
            markPoint: {
              label: {
                show: true,
                fontSize: 10,
                formatter: '{c}',
              },
              symbol: 'pin',
              symbolSize: 40,
              symbolOffset: [0, 0],
              data: [
                {
                  value: curScore,
                  xAxis: showCount,
                  yAxis: curScore,
                },
              ],
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

import { alpha, useMantineColorScheme, useMantineTheme } from '@mantine/core'
import ReactEcharts from 'echarts-for-react'
import { FC } from 'react'

interface TeamRadarMapProps {
  indicator: { name: string; max: number }[]
  value: number[]
  name: string
}

const TeamRadarMap: FC<TeamRadarMapProps> = ({ indicator, value, name }) => {
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()

  return (
    <ReactEcharts
      theme={colorScheme}
      option={{
        animation: false,
        color: theme.colors[theme.primaryColor][5],
        backgroundColor: 'transparent',
        radar: {
          indicator,
          axisName: {
            color: colorScheme === 'dark' ? theme.colors.light[1] : theme.colors.dark[5],
          },
        },
        series: [
          {
            type: 'radar',
            data: [
              {
                value,
                name,
                areaStyle: {
                  color: alpha(theme.colors[theme.primaryColor][4], 0.6),
                },
              },
            ],
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

export default TeamRadarMap

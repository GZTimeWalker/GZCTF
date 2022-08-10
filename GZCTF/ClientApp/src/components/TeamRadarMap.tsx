import ReactEcharts from 'echarts-for-react'
import { FC } from 'react'
import { useMantineTheme } from '@mantine/core'

interface TeamRadarMapProps {
  indicator: { name: string; max: number }[]
  value: number[]
  name: string
}

const TeamRadarMap: FC<TeamRadarMapProps> = ({ indicator, value, name }) => {
  const theme = useMantineTheme()

  return (
    <ReactEcharts
      theme={theme.colorScheme}
      option={{
        animation: false,
        color: theme.colors.brand[5],
        backgroundColor: 'transparent',
        radar: {
          indicator,
          axisName: {
            color: theme.colorScheme === 'dark' ? theme.colors.white[1] : theme.colors.dark[5],
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
                  color: theme.fn.rgba(theme.colors.brand[4], 0.6),
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

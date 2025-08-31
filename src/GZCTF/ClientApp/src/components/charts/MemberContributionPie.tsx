import { useMantineColorScheme, useMantineTheme } from '@mantine/core'
import chroma from 'chroma-js'
import type { EChartsOption } from 'echarts'
import { FC, useMemo } from 'react'
import { EchartsContainer } from '@Components/charts/EchartsContainer'

export interface MemberContributionPieProps {
  data: { name: string; value: number }[]
}

export const MemberContributionPie: FC<MemberContributionPieProps> = ({ data }) => {
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()

  const labelColor = colorScheme === 'dark' ? theme.colors.light[1] : theme.colors.dark[5]
  const backgroundColor = colorScheme === 'dark' ? theme.colors.gray[6] : theme.colors.light[1]

  const colorPalette = useMemo(() => {
    const [start, end] = colorScheme === 'dark' ? [3, 7] : [4, 8]
    return chroma
      .scale([theme.colors[theme.primaryColor][start], theme.colors[theme.primaryColor][end]])
      .mode('oklch')
      .colors(data.length)
  }, [data.length, theme.primaryColor, theme.colors, colorScheme])

  const option: EChartsOption = useMemo(
    () =>
      ({
        animation: true,
        backgroundColor: 'transparent',
        color: colorPalette,
        tooltip: {
          trigger: 'item',
          formatter: '{b}: {c} ({d}%)',
          borderWidth: 0,
          textStyle: {
            fontSize: 12,
            color: labelColor,
          },
          backgroundColor: backgroundColor,
        },
        legend: {
          orient: 'horizontal',
          left: 'center',
          top: 'bottom',
          data: data.map((item) => item.name),
          textStyle: {
            color: labelColor,
            fontWeight: 'bold',
          },
        },
        series: [
          {
            type: 'pie',
            radius: ['45%', '65%'],
            center: ['50%', '45%'],
            avoidLabelOverlap: false,
            itemStyle: {
              borderRadius: 8,
              borderWidth: 0,
            },
            label: {
              show: false,
              position: 'center',
            },
            emphasis: {
              label: {
                show: true,
                fontSize: 14,
                fontWeight: 'bold',
              },
            },
            labelLine: {
              show: false,
            },
            data,
          },
        ],
      }) satisfies EChartsOption,
    [data, colorPalette, labelColor, backgroundColor]
  )

  return (
    <EchartsContainer
      option={option}
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

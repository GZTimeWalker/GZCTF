import { useMantineColorScheme } from '@mantine/core'
import type { EChartsOption } from 'echarts'
import * as echarts from 'echarts'
import { FC, useEffect, useRef } from 'react'

export interface EchartsContainerProps extends React.ComponentPropsWithoutRef<'div'> {
  option: EChartsOption
  opts?: echarts.EChartsInitOpts
  style?: React.CSSProperties
}

export const EchartsContainer: FC<EchartsContainerProps> = (props) => {
  const chartRef = useRef<HTMLDivElement>(null)
  const chartInstance = useRef<echarts.ECharts | null>(null)
  const { option, opts, style, ...rest } = props

  const { colorScheme } = useMantineColorScheme()

  useEffect(() => {
    if (chartRef.current && !chartInstance.current) {
      chartInstance.current = echarts.init(chartRef.current, colorScheme === 'dark' ? 'dark' : 'default', opts)
      chartInstance.current.setOption(option)
    }

    return () => {
      if (chartInstance.current) {
        chartInstance.current.dispose()
        chartInstance.current = null
      }
    }
  }, [])

  useEffect(() => {
    if (chartInstance.current) {
      chartInstance.current.dispose()
    }
    chartInstance.current = echarts.init(chartRef.current, colorScheme === 'dark' ? 'dark' : 'default', opts)
    chartInstance.current.setOption(option)
  }, [colorScheme])

  useEffect(() => {
    if (chartInstance.current) {
      chartInstance.current.setOption(option, true)
    }
  }, [option])

  useEffect(() => {
    const handleResize = () => {
      if (chartInstance.current) {
        chartInstance.current.resize()
      }
    }

    window.addEventListener('resize', handleResize)

    return () => {
      window.removeEventListener('resize', handleResize)
    }
  }, [])

  return <div ref={chartRef} style={style} {...rest} />
}

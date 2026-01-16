import { useMantineColorScheme } from '@mantine/core'
import type { EChartsOption } from 'echarts'
import * as echarts from 'echarts'
import { FC, useEffect, useRef } from 'react'

export interface EchartsContainerProps extends React.ComponentPropsWithoutRef<'div'> {
  option: EChartsOption
  opts?: echarts.EChartsInitOpts
  style?: React.CSSProperties
  onEvents?: Record<string, (params: any) => void>
}

export const EchartsContainer: FC<EchartsContainerProps> = (props) => {
  const chartRef = useRef<HTMLDivElement>(null)
  const chartInstance = useRef<echarts.ECharts | null>(null)
  const { option, opts, style, onEvents, ...rest } = props

  const { colorScheme } = useMantineColorScheme()

  const bindEvents = (instance: echarts.ECharts, events?: Record<string, (params: any) => void>) => {
    if (!events) return
    Object.entries(events).forEach(([eventName, handler]) => {
      instance.off(eventName)
      instance.on(eventName, handler)
    })
  }

  useEffect(() => {
    if (chartRef.current && !chartInstance.current) {
      chartInstance.current = echarts.init(chartRef.current, colorScheme === 'dark' ? 'dark' : 'default', opts)
      chartInstance.current.setOption(option)
      bindEvents(chartInstance.current, onEvents)
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
    bindEvents(chartInstance.current, onEvents)
  }, [colorScheme])

  useEffect(() => {
    if (chartInstance.current) {
      chartInstance.current.setOption(option, true)
      // Re-binding events might be needed if handlers change, but usually they are stable.
      // Ideally we should have a separate effect for onEvents if they change frequentely.
      // For now, simpler is okay.
      bindEvents(chartInstance.current, onEvents)
    }
  }, [option, onEvents])

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

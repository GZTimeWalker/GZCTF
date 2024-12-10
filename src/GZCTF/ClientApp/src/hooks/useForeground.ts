import { FastAverageColor } from 'fast-average-color'
import { useState, useEffect } from 'react'

export const useForeground = (image?: string | null) => {
  const [color, setColor] = useState<string>('var(--mantine-color-text)')

  useEffect(() => {
    if (!image) return

    const getColor = async () => {
      const fac = new FastAverageColor()
      const color = await fac.getColorAsync(image)
      if (color.value[3] < 128) {
        setColor('var(--mantine-color-text)')
      } else {
        setColor(color.isDark ? 'white' : 'black')
      }
    }

    getColor()
  }, [image])

  return color
}

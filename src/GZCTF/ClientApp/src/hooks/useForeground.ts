import { FastAverageColor } from 'fast-average-color'
import { useState, useEffect } from 'react'

export const useForeground = (image?: string | null) => {
  const [color, setColor] = useState<string>('var(--mantine-color-text)')

  useEffect(() => {
    if (!image) return

    const fac = new FastAverageColor()
    fac.getColorAsync(image).then((color) => {
      if (color.value[3] < 128) {
        setColor('var(--mantine-color-text)')
      } else {
        setColor(color.isDark ? 'white' : 'black')
      }
    })
  }, [image])

  return color
}

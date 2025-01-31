import { FastAverageColor } from 'fast-average-color'
import { useState, useEffect } from 'react'

type RenderingCtx = CanvasRenderingContext2D | OffscreenCanvasRenderingContext2D

export const useForeground = (image?: string | null) => {
  const [color, setColor] = useState<string>('var(--mantine-color-text)')

  useEffect(() => {
    if (!image) return

    const getColor = async () => {
      const img = new Image()
      img.src = image
      img.crossOrigin = 'anonymous'
      await new Promise((resolve) => {
        img.onload = resolve
      })

      const imgWidth = img.width
      const imgHeight = Math.ceil(img.height * 0.2)

      const canvas =
        typeof OffscreenCanvas !== 'undefined'
          ? new OffscreenCanvas(imgWidth, imgHeight)
          : document.createElement('canvas')

      const ctx = canvas.getContext('2d') as RenderingCtx | null
      if (!ctx) return

      ctx.drawImage(img, 0, img.height - imgHeight, imgWidth, imgHeight, 0, 0, imgWidth, imgHeight)

      const fac = new FastAverageColor()
      const color = await fac.getColorAsync(canvas)
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

import { Box, Text, type MantineSize } from '@mantine/core'
import { FC, useEffect, useRef, useState } from 'react'
import classes from '@Styles/ScrollingText.module.css'

interface ScrollingTextProps {
  text: string
  onClick?: () => void
  size?: MantineSize
}

export const ScrollingText: FC<ScrollingTextProps> = ({ text, onClick, size }) => {
  const containerRef = useRef<HTMLDivElement>(null)
  const textRef = useRef<HTMLDivElement>(null)
  const [shouldScroll, setShouldScroll] = useState(false)
  const [textWidth, setTextWidth] = useState<number | null>(null)
  const [containerWidth, setContainerWidth] = useState<number | null>(null)

  useEffect(() => {
    if (textRef.current) {
      const width = textRef.current.scrollWidth
      setTextWidth(width)
    }
  }, [text])

  useEffect(() => {
    if (containerRef.current) {
      const resizeObserver = new ResizeObserver((entries) => {
        for (const entry of entries) {
          setContainerWidth(entry.contentRect.width)
        }
      })
      resizeObserver.observe(containerRef.current)
      return () => resizeObserver.disconnect()
    }
  }, [])

  useEffect(() => {
    if (textWidth !== null && containerWidth !== null) {
      setShouldScroll(textWidth > containerWidth)
    }
  }, [textWidth, containerWidth])

  const baseDuration = 4
  const widthPerSecond = 50
  const maxDuration = 12

  const dynamicDuration =
    textWidth !== null ? Math.min(baseDuration + Math.floor(textWidth / widthPerSecond), maxDuration) : baseDuration

  return (
    <Box
      ref={containerRef}
      className={classes.container}
      onClick={onClick}
      __vars={{
        '--scroll-time': `${dynamicDuration}s`,
      }}
    >
      <div className={`${classes.textWrapper} ${shouldScroll ? classes.scroll : ''}`}>
        <Text ref={textRef} className={classes.text} title={text} fz={size}>
          {text}
          <span className={classes.separator}>&nbsp;&nbsp;&nbsp;</span>
        </Text>
        {shouldScroll && (
          <>
            <Text className={classes.text} title={text} fz={size}>
              {text}
              <span className={classes.separator}>&nbsp;&nbsp;&nbsp;</span>
            </Text>
          </>
        )}
      </div>
    </Box>
  )
}

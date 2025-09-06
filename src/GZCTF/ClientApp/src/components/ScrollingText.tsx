import { Box, BoxProps, Text, type MantineSize } from '@mantine/core'
import cx from 'clsx'
import { FC, useRef, useState, useCallback } from 'react'
import classes from '@Styles/ScrollingText.module.css'

interface ScrollingTextProps extends BoxProps {
  text: string
  onClick?: () => void
  size?: MantineSize
  speedCharPerSec?: number
}

export const ScrollingText: FC<ScrollingTextProps> = ({ text, onClick, size, speedCharPerSec = 3.2, ...boxProps }) => {
  const containerRef = useRef<HTMLDivElement>(null)
  const textRef = useRef<HTMLDivElement>(null)
  const [overflow, setOverflow] = useState(false)
  const [measured, setMeasured] = useState(false)
  const [duration, setDuration] = useState(4)

  const handleMeasure = useCallback(() => {
    if (measured) return

    const c = containerRef.current
    const t = textRef.current
    if (!c || !t) return

    const fontSize = parseFloat(getComputedStyle(t).fontSize || '14') || 14
    const textWidth = t.scrollWidth
    const extra = textWidth - c.clientWidth
    const isOverflow = extra > 0

    if (isOverflow) {
      const duration = textWidth / (speedCharPerSec * fontSize)
      setDuration(Math.max(3, duration))
      setOverflow(true)
    }
    setMeasured(true)
  }, [measured, speedCharPerSec])

  return (
    <Box
      ref={containerRef}
      className={classes.container}
      onClick={onClick}
      onMouseEnter={handleMeasure}
      data-scroll={overflow || undefined}
      __vars={{ '--scroll-time': `${duration}s` }}
      {...boxProps}
    >
      <div className={classes.textWrapper}>
        <Text ref={textRef} className={classes.text} title={text} fz={size}>
          {text}
        </Text>
        {overflow && (
          <Text className={cx(classes.text, classes.clone)} fz={size} aria-hidden>
            {text}
          </Text>
        )}
      </div>
    </Box>
  )
}

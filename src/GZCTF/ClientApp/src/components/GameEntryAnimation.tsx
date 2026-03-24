import { CSSProperties, FC, useEffect, useMemo, useState } from 'react'
import { LogoBox } from '@Components/LogoBox'
import { consumeGameTransitionState } from '@Utils/gameTransition'
import classes from '@Styles/GameEntryAnimation.module.css'

interface GameEntryAnimationProps {
  gameId: number
  poster?: string | null
  onCompleted?: () => void
}

type Phase = 'idle' | 'expand' | 'logo' | 'exit' | 'done'

type IntroStyle = CSSProperties & {
  '--intro-top': string
  '--intro-left': string
  '--intro-width': string
  '--intro-height': string
  '--intro-translate-x': string
  '--intro-translate-y': string
  '--intro-scale': string
  '--intro-initial-radius': string
  '--intro-target-radius': string
}

export const GameEntryAnimation: FC<GameEntryAnimationProps> = ({ gameId, poster, onCompleted }) => {
  const [phase, setPhase] = useState<Phase>('idle')

  const transitionState = useMemo(() => consumeGameTransitionState(gameId), [gameId])

  const computedStyle = useMemo(() => {
    if (!transitionState) {
      return null
    }

    if (typeof window === 'undefined') {
      return null
    }

    const { top, left, width, height } = transitionState

    const initialRadius = Math.max(width, height) / 2
    const diagonal = Math.hypot(window.innerWidth, window.innerHeight)
    const targetRadius = diagonal

    const translateX = window.innerWidth / 2 - (left + width / 2)
    const translateY = window.innerHeight / 2 - (top + height / 2)

    // Adjust scale to reduce zoom so that at least ~90% of poster is visible
    const scale = initialRadius === 0 ? 1 : (targetRadius / initialRadius) * 1.0

    const style: IntroStyle = {
      '--intro-top': `${top}px`,
      '--intro-left': `${left}px`,
      '--intro-width': `${width}px`,
      '--intro-height': `${height}px`,
      '--intro-translate-x': `${translateX}px`,
      '--intro-translate-y': `${translateY}px`,
      '--intro-scale': `${scale}`,
      '--intro-initial-radius': `${initialRadius}px`,
      '--intro-target-radius': `${targetRadius}px`,
    }

    return style
  }, [transitionState])

  useEffect(() => {
    if (!transitionState) {
      setPhase('done')
      return
    }

    setPhase('idle')
    const raf = requestAnimationFrame(() => {
      setPhase('expand')
    })

    const logoTimer = window.setTimeout(() => {
      setPhase('logo')
    }, 600)

    const exitTimer = window.setTimeout(() => {
      setPhase('exit')
    }, 1800)

    const doneTimer = window.setTimeout(() => {
      setPhase('done')
    }, 2000)

    return () => {
      cancelAnimationFrame(raf)
      window.clearTimeout(logoTimer)
      window.clearTimeout(exitTimer)
      window.clearTimeout(doneTimer)
    }
  }, [transitionState])

  useEffect(() => {
    if (phase === 'done' && onCompleted) {
      onCompleted()
    }
  }, [phase, onCompleted])

  if (!transitionState || !computedStyle || phase === 'done') {
    return null
  }

  const appliedPoster = poster ?? transitionState.poster ?? ''

  return (
    <div className={classes.overlay} data-phase={phase} style={computedStyle}>
      <div className={classes.poster} style={{ backgroundImage: appliedPoster ? `url("${appliedPoster}")` : undefined }} />
      <div className={classes.contentWrapper}>
        <div className={classes.logoWrapper}>
          <LogoBox size="5rem" className={classes.logo} />
        </div>
        <div className={classes.title}>第四届“山城杯”大学生网络安全联赛</div>
      </div>
    </div>
  )
}
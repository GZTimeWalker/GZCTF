import {
  BoxProps,
  Center,
  Group,
  MantineColor,
  em,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import cx from 'clsx'
import { FC } from 'react'
import classes from '@Styles/GameProgress.module.css'

export interface GameProgressProps extends BoxProps {
  thickness?: number
  spikeLength?: number
  percentage: number
  color?: MantineColor
}

const GameProgress: FC<GameProgressProps> = (props: GameProgressProps) => {
  const { thickness = 4, spikeLength = 250, percentage, color, ...others } = props

  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()

  const pulsing = percentage < 100
  const resolvedColor = pulsing
    ? colorScheme === 'dark'
      ? 'light'
      : (color ?? theme.primaryColor)
    : 'gray'
  const spikeColor = theme.colors[resolvedColor][5]
  const bgColor = theme.colors[resolvedColor][2]

  return (
    <Center
      py={(thickness * spikeLength) / 100}
      {...others}
      __vars={{
        '--thickness': em(thickness),
        '--spike-length': `${spikeLength}%`,
        '--neg-spike-length': `${-spikeLength}%`,
        '--percentage': `${percentage}%`,
        '--spike-color': spikeColor,
        '--bg-color': bgColor,
        '--pulsing-display': pulsing ? 'block' : 'none',
      }}
    >
      <div className={classes.back}>
        <Group justify="right" className={classes.box}>
          <div className={classes.bar}>
            <div />
          </div>
          <div className={classes.spikes}>
            <div className={cx(classes.spike, classes.r)} />
            <div className={cx(classes.spike, classes.l)} />
            <div className={cx(classes.spike, classes.t)} />
            <div className={cx(classes.spike, classes.b)} />
          </div>
        </Group>
      </div>
    </Center>
  )
}

export default GameProgress

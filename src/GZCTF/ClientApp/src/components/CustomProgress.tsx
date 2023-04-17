import { FC } from 'react'
import { BoxProps, Center, createStyles, Group, keyframes, MantineColor } from '@mantine/core'

export interface CustomProgressProps extends BoxProps {
  thickness?: number
  spikeLength?: number
  percentage: number
  color?: MantineColor
}

export const useStyles = createStyles(
  (theme, { spikeLength = 250, color, percentage, thickness = 4 }: CustomProgressProps) => {
    const _color =
      percentage < 100 ? (theme.colorScheme === 'dark' ? 'white' : color ?? 'brand') : 'gray'
    const spikeColor = theme.fn.rgba(theme.colors[_color][5], 0.75)
    const barColor = theme.colorScheme === 'dark' ? theme.colors.white[9] : theme.colors[_color][2]
    const spikeLengthStr = `${spikeLength}%`
    const negSpikeLengthStr = `-${spikeLength}%`
    const pulsing = percentage < 100

    return {
      spikesGroup: {
        display: pulsing ? 'block' : 'none',
        position: 'relative',
        height: '100%',
        aspectRatio: '1 / 1',
        backgroundColor:
          theme.colorScheme === 'dark' ? theme.colors.white[0] : theme.colors[_color][5],

        '& div': {
          animation: `${keyframes`0% {opacity: .3;} 100% {opacity: 1;}`} 2s linear 0s infinite alternate`,
        },
      },
      progressPulseContainer: {
        display: pulsing ? 'block' : 'none',
        position: 'absolute',
        width: '100%',
        height: '100%',

        '& div': {
          width: '25%',
          height: '100%',
          background: `linear-gradient(-90deg, ${spikeColor}, #fff0)`,
          animation: `${keyframes`0% { width: 0%;} 80% {opacity: 1; width: 100%;} 100% {opacity: 0; width: 100%;}`} 2s linear 0s infinite normal`,
        },
      },
      progressBar: {
        position: 'relative',
        height: '100%',
        width: `${percentage}%`,
        minWidth: thickness,
        backgroundColor: theme.fn.rgba(barColor, 0.7),
      },
      progressBackground: {
        display: 'flex',
        alignItems: 'center',
        height: thickness,
        width: '100%',
        backgroundColor: theme.fn.rgba(
          theme.colorScheme === 'dark' ? theme.colors.gray[6] : theme.colors.white[4],
          0.8
        ),
      },
      spike: {
        position: 'absolute',
      },
      spikeLeft: {
        left: 0,
        top: negSpikeLengthStr,
        height: spikeLengthStr,
        width: '100%',
        background: `linear-gradient(0deg, ${spikeColor}, #fff0)`,
      },
      spikeRight: {
        left: 0,
        bottom: negSpikeLengthStr,
        height: spikeLengthStr,
        width: '100%',
        background: `linear-gradient(180deg, ${spikeColor}, #fff0)`,
      },
      spikeTop: {
        right: negSpikeLengthStr,
        top: 0,
        height: '100%',
        width: spikeLengthStr,
        background: `linear-gradient(90deg, ${spikeColor}, #fff0)`,
      },
      spikeBottom: {
        left: negSpikeLengthStr,
        top: 0,
        height: '100%',
        width: spikeLengthStr,
        background: `linear-gradient(-90deg, ${spikeColor}, #fff0)`,
      },
    }
  }
)

const CustomProgress: FC<CustomProgressProps> = (props: CustomProgressProps) => {
  const { thickness = 4, spikeLength = 250, ...others } = props

  const { classes, cx } = useStyles(props)

  return (
    <Center py={(thickness * spikeLength) / 100} {...others}>
      <div className={classes.progressBackground}>
        <Group position="right" className={classes.progressBar}>
          <div className={classes.progressPulseContainer}>
            <div />
          </div>
          <div className={classes.spikesGroup}>
            <div className={cx(classes.spike, classes.spikeRight)} />
            <div className={cx(classes.spike, classes.spikeLeft)} />
            <div className={cx(classes.spike, classes.spikeTop)} />
            <div className={cx(classes.spike, classes.spikeBottom)} />
          </div>
        </Group>
      </div>
    </Center>
  )
}

export default CustomProgress

import dayjs from 'dayjs'
import { FC } from 'react'
import {
  Card,
  Divider,
  Group,
  Tooltip,
  Stack,
  Text,
  Title,
  createStyles,
  keyframes,
} from '@mantine/core'
import { mdiFlag } from '@mdi/js'
import { Icon } from '@mdi/react'
import { useTooltipStyles } from '@Utils/ThemeOverride'
import { ChallengeInfo, SubmissionType } from '@Api'
import { BloodsTypes, ChallengeTagLabelMap } from '../utils/ChallengeItem'

interface ChallengeCardProps {
  challenge: ChallengeInfo
  solved?: boolean
  onClick?: () => void
  iconMap: Map<SubmissionType, React.ReactNode>
  colorMap: Map<SubmissionType, string | undefined>
  teamId?: number
}

export const useStyles = createStyles((theme, { colorMap }: ChallengeCardProps) => ({
  spike: {
    position: 'absolute',
    left: '50%',
    top: '50%',
    transform: 'translate(-50%, -50%)',
    filter:
      theme.colorScheme === 'dark' ? 'brightness(.8) saturate(.5)' : 'brightness(1.2) saturate(.8)',
    width: '70%',
    height: '200%',
    zIndex: 91,
    animation: `${keyframes`0% {opacity: .3;} 100% {opacity: 1;}`} 2s linear 0s infinite alternate`,
  },
  blood1: {
    background: `linear-gradient(0deg, #fff0, ${colorMap.get(SubmissionType.FirstBlood)}, #fff0)`,
  },
  blood2: {
    background: `linear-gradient(0deg, #fff0, ${colorMap.get(SubmissionType.SecondBlood)}, #fff0)`,
  },
  blood3: {
    background: `linear-gradient(0deg, #fff0, ${colorMap.get(SubmissionType.ThirdBlood)}, #fff0)`,
  },
}))

const ChallengeCard: FC<ChallengeCardProps> = (props: ChallengeCardProps) => {
  const { challenge, solved, onClick, iconMap, teamId } = props

  const tagData = ChallengeTagLabelMap.get(challenge.tag!)
  const { classes, cx, theme } = useStyles(props)
  const { classes: tooltipClasses } = useTooltipStyles()
  const colorStr = theme.colors[tagData?.color ?? 'brand'][5]

  return (
    <Card
      onClick={onClick}
      radius="md"
      shadow="sm"
      sx={(theme) => ({
        transition: 'filter .1s',
        ...theme.fn.hover({
          filter: theme.colorScheme === 'dark' ? 'brightness(1.2)' : 'brightness(.97)',
          cursor: 'pointer',
        }),
      })}
    >
      <Stack spacing="sm" style={{ position: 'relative', zIndex: 99 }}>
        <Group noWrap position="apart" spacing="xs">
          <Text lineClamp={1} weight={700} size={theme.fontSizes.lg}>
            {challenge.title}
          </Text>
          {solved && <Icon path={mdiFlag} size={1} color={colorStr} />}
        </Group>
        <Divider />
        <Group noWrap position="apart" align="start">
          <Group noWrap position="center">
            <Text
              align="center"
              weight={700}
              size={18}
              sx={(theme) => ({ fontFamily: theme.fontFamilyMonospace })}
            >
              {challenge.score} pts
            </Text>
          </Group>
          <Stack spacing="xs">
            <Title order={6} align="center" style={{ marginTop: theme.spacing.xs / 2 }}>
              {`${challenge.solved} `}
              <Text color="dimmed" size="xs" inherit span>
                支队伍攻克
              </Text>
            </Title>
            <Group position="center" spacing="md" style={{ height: 20 }}>
              {challenge.bloods &&
                challenge.bloods.map((blood, idx) => (
                  <Tooltip.Floating
                    key={idx}
                    position="bottom"
                    multiline
                    classNames={tooltipClasses}
                    label={
                      <Stack spacing={0}>
                        <Text color={theme.colorScheme === 'dark' ? '' : 'dark'}>
                          {blood?.name}
                        </Text>
                        <Text size="xs" color="dimmed">
                          {dayjs(blood?.submitTimeUTC).format('YY/MM/DD HH:mm:ss')}
                        </Text>
                      </Stack>
                    }
                  >
                    <div style={{ position: 'relative', height: 20 }}>
                      <div style={{ position: 'relative', zIndex: 92 }}>
                        {iconMap.get(BloodsTypes[idx])}
                      </div>
                      <div
                        className={cx(
                          classes.spike,
                          idx == 0 ? classes.blood1 : idx == 1 ? classes.blood2 : classes.blood3
                        )}
                        style={{ display: teamId === blood?.id ? 'block' : 'none' }}
                      />
                    </div>
                  </Tooltip.Floating>
                ))}
            </Group>
          </Stack>
        </Group>
      </Stack>
      {tagData && (
        <Icon
          path={tagData.icon}
          size={4}
          color={theme.fn.rgba(theme.colors[tagData?.color][7], 0.3)}
          style={{
            position: 'absolute',
            bottom: 0,
            left: 0,
            transform: 'translateY(35%)',
            zIndex: 90,
          }}
        />
      )}
    </Card>
  )
}

export default ChallengeCard

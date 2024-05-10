import {
  Box,
  Card,
  Center,
  Code,
  Divider,
  Group,
  Stack,
  Text,
  Title,
  Tooltip,
  alpha,
  useMantineTheme,
} from '@mantine/core'
import { mdiFlag } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC } from 'react'
import { Trans } from 'react-i18next'
import { BloodsTypes, useChallengeTagLabelMap } from '@Utils/Shared'
import { ChallengeInfo, SubmissionType } from '@Api'
import classes from '@Styles/ChallengeCard.module.css'
import tooltipClasses from '@Styles/Tooltip.module.css'

interface ChallengeCardProps {
  challenge: ChallengeInfo
  solved?: boolean
  onClick?: () => void
  iconMap: Map<SubmissionType, React.ReactNode>
  colorMap: Map<SubmissionType, string | undefined>
  teamId?: number
}

const ChallengeCard: FC<ChallengeCardProps> = (props: ChallengeCardProps) => {
  const { challenge, solved, onClick, iconMap, teamId, colorMap } = props
  const challengeTagLabelMap = useChallengeTagLabelMap()
  const tagData = challengeTagLabelMap.get(challenge.tag!)
  const theme = useMantineTheme()
  const colorStr = theme.colors[tagData?.color ?? theme.primaryColor][5]

  return (
    <Card onClick={onClick} radius="md" shadow="sm" className={classes.card}>
      <Stack gap="sm" pos="relative" style={{ zIndex: 99 }}>
        <Group h="30px" wrap="nowrap" justify="space-between" gap={2}>
          <Text fw="bold" truncate fz="lg">
            {challenge.title}
          </Text>
          <Center miw="1.5em">{solved && <Icon path={mdiFlag} size={1} color={colorStr} />}</Center>
        </Group>
        <Divider />
        <Group wrap="nowrap" justify="space-between" align="center" gap={2}>
          <Text ta="center" fw="bold" fz="lg" ff="monospace">
            {challenge.score}&nbsp;pts
          </Text>
          <Stack gap="xs">
            <Title order={6} c="dimmed" ta="center" mt={`calc(${theme.spacing.xs} / 2)`}>
              <Trans
                i18nKey={'challenge.content.solved'}
                values={{
                  solved: challenge.solved,
                }}
              >
                _
                <Code fz="sm" fw="bold" bg="transparent">
                  _
                </Code>
                _
              </Trans>
            </Title>
            <Group justify="center" gap="md" h={20} wrap="nowrap">
              {challenge.bloods &&
                challenge.bloods.map((blood, idx) => (
                  <Tooltip.Floating
                    key={idx}
                    position="bottom"
                    multiline
                    classNames={tooltipClasses}
                    label={
                      <Stack gap={0}>
                        <Text fw={500}>{blood?.name}</Text>
                        <Text fw={500} size="xs" c="dimmed">
                          {dayjs(blood?.submitTimeUtc).format('YY/MM/DD HH:mm:ss')}
                        </Text>
                      </Stack>
                    }
                  >
                    <div style={{ position: 'relative', height: 20 }}>
                      <div style={{ position: 'relative', zIndex: 92 }}>
                        {iconMap.get(BloodsTypes[idx])}
                      </div>
                      <Box
                        className={classes.spike}
                        data-blood={teamId === blood?.id || undefined}
                        __vars={{
                          '--blood-color': colorMap.get(BloodsTypes[idx]),
                        }}
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
          color={alpha(theme.colors[tagData?.color][7], 0.3)}
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

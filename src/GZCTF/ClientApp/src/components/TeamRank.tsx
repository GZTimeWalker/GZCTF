import {
  Avatar,
  Badge,
  Card,
  CardProps,
  Group,
  PasswordInput,
  Progress,
  Skeleton,
  Stack,
  Text,
  Title,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiExclamationThick, mdiKey } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { ErrorCodes } from '@Utils/Shared'
import { useIsMobile } from '@Utils/ThemeOverride'
import { useGameTeamInfo } from '@Utils/useGame'

const TeamRank: FC<CardProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const navigate = useNavigate()
  const { teamInfo, error } = useGameTeamInfo(numId)

  const clipboard = useClipboard()
  const isMobile = useIsMobile(1080)

  const { t } = useTranslation()

  const solved = (teamInfo?.rank?.solvedCount ?? 0) / (teamInfo?.challengeCount ?? 1)

  useEffect(() => {
    if (error?.status === ErrorCodes.GameEnded) {
      navigate(`/games/${numId}`)
      showNotification({
        color: 'yellow',
        message: t('game.notification.ended'),
        icon: <Icon path={mdiExclamationThick} size={1} />,
      })
    }
  }, [error])

  const rank = teamInfo?.rank

  const item = (label: string, value?: null | string | number) => (
    <Stack gap={2}>
      <Skeleton visible={!rank}>
        <Text ff="monospace" fw="bold">
          {value ?? '0'}
        </Text>
      </Skeleton>
      <Text size="xs" fw={500}>
        {label}
      </Text>
    </Stack>
  )

  return (
    <Card {...props} shadow="sm">
      <Stack gap="xs">
        <Group gap="sm" wrap="nowrap">
          <Avatar alt="avatar" size={50} radius="md" src={rank?.avatar}>
            {rank?.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Skeleton visible={!rank}>
            <Stack gap={2} align="flex-start">
              <Title order={3} lineClamp={1}>
                {rank?.name ?? 'Team'}
              </Title>
              {rank?.organization && (
                <Badge size="xs" variant="outline">
                  {rank.organization}
                </Badge>
              )}
            </Stack>
          </Skeleton>
        </Group>
        <Group grow ta="center">
          {item(t('game.label.score_table.rank_total'), rank?.rank)}
          {rank?.organization &&
            item(t('game.label.score_table.rank_organization'), rank?.organizationRank)}
          {item(t('game.label.score_table.score'), rank?.score)}
          {item(t('game.label.score_table.solved_count'), rank?.solvedCount)}
        </Group>
        <Progress value={solved * 100} />
        {!isMobile && (
          <PasswordInput
            value={teamInfo?.teamToken}
            readOnly
            leftSection={<Icon path={mdiKey} size={1} />}
            variant="unstyled"
            onClick={() => {
              clipboard.copy(teamInfo?.teamToken)
              showNotification({
                color: 'teal',
                message: t('team.notification.token.copied'),
                icon: <Icon path={mdiCheck} size={1} />,
              })
            }}
            styles={(theme) => ({
              innerInput: {
                cursor: 'copy',
                fontFamily: theme.fontFamilyMonospace,
              },
            })}
          />
        )}
      </Stack>
    </Card>
  )
}

export default TeamRank

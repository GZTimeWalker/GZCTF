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
import { createStyles } from '@mantine/emotion'
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

const useStyle = createStyles((theme) => ({
  number: {
    fontFamily: theme.fontFamilyMonospace,
    fontWeight: 700,
  },
}))

const TeamRank: FC<CardProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const navigate = useNavigate()
  const { teamInfo, error } = useGameTeamInfo(numId)

  const { classes, theme } = useStyle()

  const clipboard = useClipboard()
  const isMobile = useIsMobile(1080)

  const { t } = useTranslation()

  const solved = (teamInfo?.rank?.solvedCount ?? 0) / (teamInfo?.rank?.challenges?.length ?? 1)

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

  return (
    <Card {...props} shadow="sm" p="md">
      <Stack gap={8}>
        <Group gap="sm" wrap="nowrap">
          <Avatar alt="avatar" color="cyan" size={50} radius="md" src={teamInfo?.rank?.avatar}>
            {teamInfo?.rank?.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Skeleton visible={!teamInfo}>
            <Stack gap={2} align="flex-start">
              <Title order={3} lineClamp={1}>
                {teamInfo?.rank?.name ?? 'Team'}
              </Title>
              {teamInfo?.rank?.organization && (
                <Badge size="xs" variant="outline">
                  {teamInfo.rank.organization}
                </Badge>
              )}
            </Stack>
          </Skeleton>
        </Group>
        <Group grow ta="center">
          <Stack gap={2}>
            <Skeleton visible={!teamInfo}>
              <Text className={classes.number}>{teamInfo?.rank?.rank ?? '0'}</Text>
            </Skeleton>
            <Text size="xs">{t('game.label.score_table.rank_total')}</Text>
          </Stack>
          {teamInfo?.rank?.organization && (
            <Stack gap={2}>
              <Skeleton visible={!teamInfo}>
                <Text className={classes.number}>{teamInfo?.rank?.organizationRank ?? '0'}</Text>
              </Skeleton>
              <Text size="xs">{t('game.label.score_table.rank_organization')}</Text>
            </Stack>
          )}
          <Stack gap={2}>
            <Skeleton visible={!teamInfo}>
              <Text className={classes.number}>{teamInfo?.rank?.score ?? '0'}</Text>
            </Skeleton>
            <Text size="xs">{t('game.label.score_table.score')}</Text>
          </Stack>
          <Stack gap={2}>
            <Skeleton visible={!teamInfo}>
              <Text className={classes.number}>{teamInfo?.rank?.solvedCount ?? '0'}</Text>
            </Skeleton>
            <Text size="xs">{t('game.label.score_table.solved_count')}</Text>
          </Stack>
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
            styles={{
              innerInput: {
                cursor: 'copy',
                fontFamily: theme.fontFamilyMonospace,
              },
            }}
          />
        )}
      </Stack>
    </Card>
  )
}

export default TeamRank

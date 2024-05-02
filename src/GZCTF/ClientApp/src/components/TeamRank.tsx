import {
  Avatar,
  Badge,
  Card,
  createStyles,
  Group,
  PaperProps,
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
import api from '@Api'

const useStyle = createStyles((theme) => ({
  number: {
    fontFamily: theme.fontFamilyMonospace,
    fontWeight: 700,
  },
}))

const TeamRank: FC<PaperProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const navigate = useNavigate()
  const { data, error } = api.game.useGameChallengesWithTeamInfo(numId, {
    shouldRetryOnError: false,
  })

  const { classes, theme } = useStyle()

  const clipboard = useClipboard()
  const isMobile = useIsMobile(1080)

  const { t } = useTranslation()

  const solved = (data?.rank?.solvedCount ?? 0) / (data?.rank?.challenges?.length ?? 1)

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
          <Avatar alt="avatar" color="cyan" size={50} radius="md" src={data?.rank?.avatar}>
            {data?.rank?.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Skeleton visible={!data}>
            <Stack gap={2} align="flex-start">
              <Title order={3} lineClamp={1}>
                {data?.rank?.name ?? 'Team'}
              </Title>
              {data?.rank?.organization && (
                <Badge size="xs" variant="outline">
                  {data.rank.organization}
                </Badge>
              )}
            </Stack>
          </Skeleton>
        </Group>
        <Group grow ta="center">
          <Stack gap={2}>
            <Skeleton visible={!data}>
              <Text className={classes.number}>{data?.rank?.rank ?? '0'}</Text>
            </Skeleton>
            <Text size="xs">{t('game.label.score_table.rank_total')}</Text>
          </Stack>
          {data?.rank?.organization && (
            <Stack gap={2}>
              <Skeleton visible={!data}>
                <Text className={classes.number}>{data?.rank?.organizationRank ?? '0'}</Text>
              </Skeleton>
              <Text size="xs">{t('game.label.score_table.rank_organization')}</Text>
            </Stack>
          )}
          <Stack gap={2}>
            <Skeleton visible={!data}>
              <Text className={classes.number}>{data?.rank?.score ?? '0'}</Text>
            </Skeleton>
            <Text size="xs">{t('game.label.score_table.score')}</Text>
          </Stack>
          <Stack gap={2}>
            <Skeleton visible={!data}>
              <Text className={classes.number}>{data?.rank?.solvedCount ?? '0'}</Text>
            </Skeleton>
            <Text size="xs">{t('game.label.score_table.solved_count')}</Text>
          </Stack>
        </Group>
        <Progress value={solved * 100} />
        {!isMobile && (
          <PasswordInput
            value={data?.teamToken}
            readOnly
            icon={<Icon path={mdiKey} size={1} />}
            variant="unstyled"
            onClick={() => {
              clipboard.copy(data?.teamToken)
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

import { FC, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
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

  const solved = (data?.rank?.solvedCount ?? 0) / (data?.rank?.challenges?.length ?? 1)

  useEffect(() => {
    if (error?.title?.includes('已结束')) {
      navigate(`/games/${numId}`)
      showNotification({
        color: 'yellow',
        message: '比赛已经结束',
        icon: <Icon path={mdiExclamationThick} size={1} />,
      })
    }
  }, [error])

  return (
    <Card {...props} shadow="sm" p="md">
      <Stack spacing={8}>
        <Group spacing="sm" noWrap>
          <Avatar alt="avatar" color="cyan" size={50} radius="md" src={data?.rank?.avatar}>
            {data?.rank?.name?.slice(0, 1) ?? 'T'}
          </Avatar>
          <Skeleton visible={!data}>
            <Stack spacing={2} align="flex-start">
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
          <Stack spacing={2}>
            <Skeleton visible={!data}>
              <Text className={classes.number}>{data?.rank?.rank ?? '0'}</Text>
            </Skeleton>
            <Text size="xs">总排名</Text>
          </Stack>
          {data?.rank?.organization && (
            <Stack spacing={2}>
              <Skeleton visible={!data}>
                <Text className={classes.number}>{data?.rank?.organizationRank ?? '0'}</Text>
              </Skeleton>
              <Text size="xs">排名</Text>
            </Stack>
          )}
          <Stack spacing={2}>
            <Skeleton visible={!data}>
              <Text className={classes.number}>{data?.rank?.score ?? '0'}</Text>
            </Skeleton>
            <Text size="xs">得分</Text>
          </Stack>
          <Stack spacing={2}>
            <Skeleton visible={!data}>
              <Text className={classes.number}>{data?.rank?.solvedCount ?? '0'}</Text>
            </Skeleton>
            <Text size="xs">攻克数量</Text>
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
                message: '队伍Token已复制到剪贴板',
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

import { FC, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Avatar,
  Group,
  Card,
  Stack,
  Title,
  Text,
  PaperProps,
  createStyles,
  Progress,
  Skeleton,
  PasswordInput,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiExclamationThick, mdiKey } from '@mdi/js'
import { Icon } from '@mdi/react'
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
  const { data: myteam, error } = api.game.useGameMyTeam(numId)

  const { classes, theme } = useStyle()

  const clipboard = useClipboard()

  const solved = (myteam?.rank?.solvedCount ?? 0) / (myteam?.rank?.challenges?.length ?? 1)

  useEffect(() => {
    if (error?.title?.includes('已结束')) {
      navigate(`/games/${numId}`)
      showNotification({
        color: 'yellow',
        message: '比赛已经结束',
        icon: <Icon path={mdiExclamationThick} size={1} />,
        disallowClose: true,
      })
    }
  }, [error])

  return (
    <Card shadow="sm" {...props}>
      <Stack spacing={8}>
        <Group>
          <Avatar color="cyan" size="md" radius="md" src={myteam?.rank?.avatar}>
            {myteam?.rank?.name?.at(0) ?? 'T'}
          </Avatar>
          <Skeleton width="8rem" visible={!myteam}>
            <Title order={4}>{myteam?.rank?.name ?? 'Team'}</Title>
          </Skeleton>
        </Group>
        <Group
          grow
          style={{
            textAlign: 'center',
          }}
        >
          <Stack spacing={2}>
            <Skeleton visible={!myteam}>
              <Text className={classes.number}>{myteam?.rank?.rank ?? '0'}</Text>
            </Skeleton>
            <Text size="sm">排名</Text>
          </Stack>
          <Stack spacing={2}>
            <Skeleton visible={!myteam}>
              <Text className={classes.number}>{myteam?.rank?.score ?? '0'}</Text>
            </Skeleton>
            <Text size="sm">得分</Text>
          </Stack>
          <Stack spacing={2}>
            <Skeleton visible={!myteam}>
              <Text className={classes.number}>{myteam?.rank?.solvedCount ?? '0'}</Text>
            </Skeleton>
            <Text size="sm">攻克数量</Text>
          </Stack>
        </Group>
        <Progress value={solved * 100} />
        <PasswordInput
          value={myteam?.teamToken}
          readOnly
          icon={<Icon path={mdiKey} size={1} />}
          variant="unstyled"
          onClick={() => {
            clipboard.copy(myteam?.teamToken)
            showNotification({
              color: 'teal',
              message: '队伍Token已复制到剪贴板',
              icon: <Icon path={mdiCheck} size={1} />,
              disallowClose: true,
            })
          }}
          styles={{
            innerInput: {
              fontFamily: theme.fontFamilyMonospace,
            },
          }}
        />
      </Stack>
    </Card>
  )
}

export default TeamRank

import { FC } from 'react'
import { useParams } from 'react-router-dom'
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
} from '@mantine/core'
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

  const { data: myteam } = api.game.useGameMyTeam(numId)

  const { classes } = useStyle()

  const solved = (myteam?.solvedCount ?? 0) / (myteam?.challenges?.length ?? 1)

  return (
    <Card shadow="sm" {...props}>
      <Stack>
        <Group>
          <Avatar color="cyan" size="md" radius="md" src={myteam?.avatar}>
            {myteam?.name?.at(0) ?? 'T'}
          </Avatar>
          <Skeleton width="8rem" visible={!myteam?.name}>
            <Title order={4}>{myteam?.name ?? 'Loading'}</Title>
          </Skeleton>
        </Group>
        <Group
          grow
          style={{
            textAlign: 'center',
          }}
        >
          <Stack spacing={2}>
            <Skeleton visible={!myteam?.rank}>
              <Text className={classes.number}>{myteam?.rank ?? 'Loading'}</Text>
            </Skeleton>
            <Text size="sm">排名</Text>
          </Stack>
          <Stack spacing={2}>
            <Skeleton visible={!myteam?.score}>
              <Text className={classes.number}>{myteam?.score ?? 'Loading'}</Text>
            </Skeleton>
            <Text size="sm">得分</Text>
          </Stack>
          <Stack spacing={2}>
            <Skeleton visible={!myteam?.solvedCount}>
              <Text className={classes.number}>{myteam?.solvedCount ?? 'Loading'}</Text>
            </Skeleton>
            <Text size="sm">攻克数量</Text>
          </Stack>
        </Group>
        <Progress value={solved * 100} />
      </Stack>
    </Card>
  )
}

export default TeamRank

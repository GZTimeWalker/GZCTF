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
} from '@mantine/core'
import api, { SubmissionType } from '../Api'

const useStyle = createStyles((theme) => ({
  number: {
    fontFamily: theme.fontFamilyMonospace,
    fontWeight: 700,
  },
}))

const TeamRank: FC<PaperProps> = (props) => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { data: myteam } = api.game.useGameMyTeam(numId, {
    refreshInterval: 0,
  })

  const { classes } = useStyle()

  const solved =
    (myteam?.challenges?.filter((c) => c.type !== SubmissionType.Unaccepted).length ?? 0) /
    (myteam?.challenges?.length ?? 1)

  return (
    <Card shadow="sm" {...props}>
      <Stack>
        <Group>
          <Avatar color="cyan" size="md" radius="md" src={myteam?.avatar}>
            {myteam?.name?.at(0) ?? 'T'}
          </Avatar>
          <Title order={4}>{myteam?.name}</Title>
        </Group>
        <Group
          grow
          style={{
            textAlign: 'center',
          }}
        >
          <Stack spacing={2}>
            <Text className={classes.number}>{myteam?.rank}</Text>
            <Text size="sm">排名</Text>
          </Stack>
          <Stack spacing={2}>
            <Text className={classes.number}>{myteam?.score}</Text>
            <Text size="sm">得分</Text>
          </Stack>
          <Stack spacing={2}>
            <Text className={classes.number}>{myteam?.solvedCount}</Text>
            <Text size="sm">攻克数量</Text>
          </Stack>
        </Group>
        <Progress value={solved * 100} />
      </Stack>
    </Card>
  )
}

export default TeamRank

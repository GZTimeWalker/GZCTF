import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Avatar, Group, Paper, Stack, Title, Text } from '@mantine/core'
import api from '../Api'

const TeamRank: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')

  const { data: myteam } = api.game.useGameMyTeam(numId, {
    refreshInterval: 0,
  })

  return (
    <Paper>
      <Stack>
        <Group>
          <Avatar color="cyan" size="lg" radius="md" src={myteam?.avatar}>
            {myteam?.name?.at(0) ?? 'T'}
          </Avatar>
          <Title order={4}>{myteam?.name}</Title>
        </Group>
        <Group>
          <Stack>
            <Text>{myteam?.rank}</Text>
            <Text>排名</Text>
          </Stack>
          <Stack>
            <Text>{myteam?.score}</Text>
            <Text>分数</Text>
          </Stack>
          <Stack>
            <Text>{myteam?.solvedCount}</Text>
            <Text>攻克数量</Text>
          </Stack>
        </Group>
      </Stack>
    </Paper>
  )
}

export default TeamRank

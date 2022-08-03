import { FC } from 'react'
import { useParams } from 'react-router-dom'
import { Avatar, Group, Card, Stack, Title, Text, PaperProps, createStyles } from '@mantine/core'
import api from '../Api'

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

  return (
    <Card
      style={{
        maxWidth: '20rem',
      }}
      {...props}
    >
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
            <Text>排名</Text>
          </Stack>
          <Stack spacing={2}>
            <Text className={classes.number}>{myteam?.score}</Text>
            <Text>分数</Text>
          </Stack>
          <Stack spacing={2}>
            <Text className={classes.number}>{myteam?.solvedCount}</Text>
            <Text>攻克数量</Text>
          </Stack>
        </Group>
      </Stack>
    </Card>
  )
}

export default TeamRank

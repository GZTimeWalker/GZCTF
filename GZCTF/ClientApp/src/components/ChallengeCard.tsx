import { FC } from 'react'
import { Badge, Card, Divider, Group, Stack, Text, Title } from '@mantine/core'
import { ChallengeInfo } from '../Api'
import { ChallengeTagLabelMap } from './ChallengeItem'

interface ChallengeCardProps {
  challenge: ChallengeInfo
  solved?: boolean
  onClick?: () => void
}

const ChallengeCard: FC<ChallengeCardProps> = ({ challenge, solved, ...others }) => {
  const noSolved = challenge.bloods?.filter((b) => b !== null).length === 0

  console.log(challenge)

  return (
    <Card
      {...others}
      shadow="sm"
      radius="md"
      sx={(theme) => ({
        transition: 'filter .1s',
        borderColor: ``,
        '&:hover': {
          filter: theme.colorScheme === 'dark' ? 'brightness(1.2)' : 'brightness(.97)',
          cursor: 'pointer',
        },
      })}
    >
      <Stack spacing="xs">
        <Group noWrap position="apart">
          <Title order={4} align="left">
            {challenge.title}
          </Title>
          <Badge color={solved ? 'yellow' : 'gray'}>{solved ? '已攻克' : '未攻克'}</Badge>
        </Group>
        <Divider
          size="sm"
          variant="dashed"
          color={ChallengeTagLabelMap.get(challenge.tag!)?.color}
        />
        <Group position="center">
          <Text
            align="center"
            weight={700}
            size={20}
            sx={(theme) => ({ fontFamily: theme.fontFamilyMonospace })}
          >
            {challenge.score} pts
          </Text>
        </Group>
        <Group position="center" style={{ height: '1.2rem' }}>
          {noSolved && (
            <Text color="dimmed" size="xs">
              还没有队伍解出此题
            </Text>
          )}
        </Group>
      </Stack>
    </Card>
  )
}

export default ChallengeCard

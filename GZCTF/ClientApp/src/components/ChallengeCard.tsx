import { FC } from 'react'
import { Card, createStyles, Divider, Group, Stack, Text, Title } from '@mantine/core'
import { Icon } from '@mdi/react'
import { ChallengeInfo } from '../Api'
import { ChallengeTagLabelMap } from './ChallengeItem'

const useStyles = createStyles((theme, _param, getRef) => {
  const solved = { ref: getRef('solved') } as const

  return {
    solved,
    indicator: {
      display: 'none',
      background: 'transparent',
      transform: 'translateY(-8px) translateX(30px) rotate(30deg)',

      [`&.${solved.ref}`]: {
        display: 'flex',
      },
    },
  }
})

interface ChallengeCardProps {
  challenge: ChallengeInfo
  solved?: boolean
  onClick?: () => void
}

const ChallengeCard: FC<ChallengeCardProps> = ({ challenge, onClick }) => {
  const tagData = ChallengeTagLabelMap.get(challenge.tag!)
  const { theme } = useStyles()

  return (
    <Card
      onClick={onClick}
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
      <Stack spacing={3}>
        <Group noWrap position="apart" spacing="xs">
          <Text lineClamp={1} weight={700} size={theme.fontSizes.lg}>
            {challenge.title}
          </Text>
        </Group>
        <Divider
          size="sm"
          variant="dashed"
          color={tagData?.color}
          labelPosition="center"
          label={tagData && <Icon path={tagData.icon} size={1} />}
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
          <Title order={6}>
            {`${challenge.solved} `}
            <Text color="dimmed" size="xs" inherit component="span">
              支队伍攻克
            </Text>
          </Title>
        </Group>
      </Stack>
    </Card>
  )
}

export default ChallengeCard

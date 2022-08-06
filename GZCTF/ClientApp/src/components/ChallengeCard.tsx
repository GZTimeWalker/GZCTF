import { ChallengeInfo } from '@Api/Api'
import dayjs from 'dayjs'
import { FC } from 'react'
import { Card, createStyles, Divider, Group, Tooltip, Stack, Text, Title } from '@mantine/core'
import { mdiFlag, mdiHexagonOutline, mdiHexagonSlice4, mdiHexagonSlice6 } from '@mdi/js'
import { Icon } from '@mdi/react'
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

const ChallengeCard: FC<ChallengeCardProps> = ({ challenge, solved, onClick }) => {
  const tagData = ChallengeTagLabelMap.get(challenge.tag!)
  const { theme } = useStyles()

  const colorStr = theme.colors[tagData?.color ?? 'brand'][5]

  return (
    <Card
      onClick={onClick}
      radius="md"
      shadow="md"
      sx={(theme) => ({
        transition: 'filter .1s',
        ...theme.fn.hover({
          filter: theme.colorScheme === 'dark' ? 'brightness(1.2)' : 'brightness(.97)',
          cursor: 'pointer',
        }),
      })}
    >
      <Stack spacing={3}>
        <Group noWrap position="apart" spacing="xs">
          <Text lineClamp={1} weight={700} size={theme.fontSizes.lg}>
            {challenge.title}
          </Text>
          {solved && <Icon path={mdiFlag} size={1} color={colorStr} />}
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
        <Title order={6} align="center">
          {`${challenge.solved} `}
          <Text color="dimmed" size="xs" inherit component="span">
            支队伍攻克
          </Text>
        </Title>
        <Group position="center" spacing="md" style={{ height: 20 }}>
          {challenge.bloods &&
            challenge.bloods.map((blood, idx) => (
              <Tooltip.Floating
                position="bottom"
                multiline
                label={
                  <Stack spacing={0}>
                    <Text>{blood?.name}</Text>
                    <Text size="xs" color="dimmed">
                      {dayjs(blood?.submitTimeUTC).format('YY/MM/DD HH:mm:ss')}
                    </Text>
                  </Stack>
                }
              >
                <Icon
                  path={
                    idx === 0 ? mdiHexagonSlice6 : idx === 1 ? mdiHexagonSlice4 : mdiHexagonOutline
                  }
                  color={theme.colors.yellow[theme.colorScheme === 'dark' ? 7 - idx : 5 + idx]}
                  size={0.8}
                />
              </Tooltip.Floating>
            ))}
        </Group>
      </Stack>
    </Card>
  )
}

export default ChallengeCard

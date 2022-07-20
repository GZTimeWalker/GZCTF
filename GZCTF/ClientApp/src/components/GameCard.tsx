import { FC } from 'react'
import { Badge, Box, Card, Group, Image, Stack, Text, Title, useMantineTheme } from '@mantine/core'
import { mdiFlagOutline } from '@mdi/js'
import Icon from '@mdi/react'
import { BasicGameInfoModel } from '../Api'

const GameCard: FC<BasicGameInfoModel> = (game) => {
  const theme = useMantineTheme()

  const { summary, title, poster, start, end } = game
  const startTime = new Date(start!)
  const endTime = new Date(end!)

  return (
    <Card
      shadow="sm"
      sx={(theme) => ({
        cursor: 'pointer',
        transition: 'filter .2s',
        '&:hover': {
          filter: theme.colorScheme === 'dark' ? 'brightness(1.2)' : 'brightness(.97)',
        },
      })}
    >
      <Card.Section>
        {poster ? (
          <Image src={poster} height={160} alt="poster" />
        ) : (
          <Box
            style={{ height: 160, display: 'flex', alignItems: 'center', justifyContent: 'center' }}
          >
            <Icon path={mdiFlagOutline} size={4} color={theme.colors.gray[5]} />
          </Box>
        )}
      </Card.Section>
      <Stack style={{ flexGrow: 1 }}>
        <Group align="end" position="apart">
          <Title order={2} align="left">
            {title}
          </Title>
          <Text size="md">
            <Badge color="brand" variant="light">
              {startTime.toLocaleString()}
            </Badge>
            ~
            <Badge color="brand" variant="light">
              {endTime.toLocaleString()}
            </Badge>
          </Text>
        </Group>
        <Text size="md" lineClamp={1}>
          {summary}
        </Text>
      </Stack>
    </Card>
  )
}

export default GameCard

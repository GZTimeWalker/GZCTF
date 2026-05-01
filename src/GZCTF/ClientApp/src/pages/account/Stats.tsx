import {
  Badge,
  Center,
  Group,
  Loader,
  Paper,
  ScrollArea,
  SimpleGrid,
  Stack,
  Table,
  Text,
  ThemeIcon,
  Title,
} from '@mantine/core'
import { mdiStar, mdiTrophy, mdiPuzzle, mdiFire } from '@mdi/js'
import { Icon } from '@mdi/react'
import type { EChartsOption } from 'echarts'
import dayjs from 'dayjs'
import { FC, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import useSWR from 'swr'
import { EchartsContainer } from '@Components/charts/EchartsContainer'
import { WithNavBar } from '@Components/WithNavbar'
import { usePageTitle } from '@Hooks/usePageTitle'
import { useLanguage } from '@Utils/I18n'
import { useMantineTheme, useMantineColorScheme } from '@mantine/core'

interface UserStatsModel {
  totalSolves: number
  totalFirstBloods: number
  gamesParticipated: number
  solvesByCategory: Record<string, number>
  games: { gameId: number; gameTitle: string; endTimeUtc: string; solves: number }[]
}

const CATEGORY_COLORS: Record<string, string> = {
  Misc: 'gray', Crypto: 'violet', Pwn: 'red', Web: 'blue',
  Reverse: 'orange', Blockchain: 'teal', Forensics: 'green',
  Hardware: 'cyan', Mobile: 'pink', PPC: 'lime', AI: 'yellow',
  Pentest: 'indigo', OSINT: 'grape',
}

const fetcher = (url: string) =>
  fetch(url, { credentials: 'include' }).then((r) => r.json())

const Stats: FC = () => {
  const { t } = useTranslation()
  const { locale } = useLanguage()
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()

  usePageTitle(t('account.title.stats', 'My Stats'))

  const { data: stats, isLoading } = useSWR<UserStatsModel>('/api/account/stats', fetcher)

  const categoryChartOption = useMemo((): EChartsOption => {
    if (!stats) return {}
    const entries = Object.entries(stats.solvesByCategory).sort((a, b) => b[1] - a[1])
    return {
      backgroundColor: 'transparent',
      tooltip: { trigger: 'axis' },
      grid: { left: 80, right: 20, top: 20, bottom: 20 },
      xAxis: { type: 'value', minInterval: 1 },
      yAxis: { type: 'category', data: entries.map(([cat]) => cat) },
      series: [{
        type: 'bar',
        data: entries.map(([, count]) => count),
        itemStyle: {
          color: (params: any) => {
            const cat = entries[params.dataIndex][0]
            return theme.colors[CATEGORY_COLORS[cat] ?? 'blue'][5]
          },
        },
        label: { show: true, position: 'right' },
      }],
    }
  }, [stats, theme, colorScheme])

  if (isLoading) {
    return (
      <WithNavBar minWidth={0}>
        <Center h="80vh"><Loader /></Center>
      </WithNavBar>
    )
  }

  if (!stats) return null

  return (
    <WithNavBar minWidth={0}>
      <Stack p="md" maw={960} mx="auto" gap="lg">
        <Title order={3}>{t('account.title.stats', 'My Stats')}</Title>

        {/* Summary cards */}
        <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="md">
          <Paper p="md" withBorder radius="md" ta="center">
            <ThemeIcon size="xl" color="teal" variant="light" mx="auto" mb="xs">
              <Icon path={mdiPuzzle} size={1.2} />
            </ThemeIcon>
            <Text size="2rem" fw={700} c="teal">{stats.totalSolves}</Text>
            <Text size="sm" c="dimmed">{t('account.stats.total_solves', 'Total Solves')}</Text>
          </Paper>
          <Paper p="md" withBorder radius="md" ta="center">
            <ThemeIcon size="xl" color="orange" variant="light" mx="auto" mb="xs">
              <Icon path={mdiFire} size={1.2} />
            </ThemeIcon>
            <Text size="2rem" fw={700} c="orange">{stats.totalFirstBloods}</Text>
            <Text size="sm" c="dimmed">{t('account.stats.first_bloods', 'First Bloods')}</Text>
          </Paper>
          <Paper p="md" withBorder radius="md" ta="center">
            <ThemeIcon size="xl" color="blue" variant="light" mx="auto" mb="xs">
              <Icon path={mdiTrophy} size={1.2} />
            </ThemeIcon>
            <Text size="2rem" fw={700} c="blue">{stats.gamesParticipated}</Text>
            <Text size="sm" c="dimmed">{t('account.stats.games', 'Games Played')}</Text>
          </Paper>
        </SimpleGrid>

        {/* Category breakdown chart */}
        {Object.keys(stats.solvesByCategory).length > 0 && (
          <Paper p="md" withBorder radius="md">
            <Text fw={600} mb="sm">{t('account.stats.by_category', 'Solves by Category')}</Text>
            <EchartsContainer
              option={categoryChartOption}
              style={{ height: Math.max(160, Object.keys(stats.solvesByCategory).length * 36) }}
            />
          </Paper>
        )}

        {/* Games table */}
        {stats.games.length > 0 && (
          <Paper p="md" withBorder radius="md">
            <Text fw={600} mb="sm">{t('account.stats.game_history', 'Game History')}</Text>
            <ScrollArea>
              <Table striped highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>{t('common.label.game')}</Table.Th>
                    <Table.Th>{t('common.label.time')}</Table.Th>
                    <Table.Th ta="right">{t('account.stats.solves', 'Solves')}</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {stats.games.map((g) => (
                    <Table.Tr key={g.gameId}>
                      <Table.Td>
                        <Link to={`/games/${g.gameId}`} style={{ textDecoration: 'none', color: 'inherit' }}>
                          <Group gap="xs">
                            <Icon path={mdiStar} size={0.7} color={theme.colors.yellow[5]} />
                            <Text size="sm">{g.gameTitle}</Text>
                          </Group>
                        </Link>
                      </Table.Td>
                      <Table.Td>
                        <Badge variant="light" color="gray" size="sm">
                          {dayjs(g.endTimeUtc).locale(locale).format('YYYY-MM-DD')}
                        </Badge>
                      </Table.Td>
                      <Table.Td ta="right">
                        <Text size="sm" fw={600} c="teal">{g.solves}</Text>
                      </Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </ScrollArea>
          </Paper>
        )}

        {stats.totalSolves === 0 && (
          <Center h="30vh">
            <Stack align="center" gap="xs">
              <Icon path={mdiPuzzle} size={3} color={theme.colors.gray[5]} />
              <Text c="dimmed">{t('account.stats.empty', 'No solves yet — go play some CTFs!')}</Text>
            </Stack>
          </Center>
        )}
      </Stack>
    </WithNavBar>
  )
}

export default Stats

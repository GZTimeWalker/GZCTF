import {
  Badge,
  Center,
  Group,
  Loader,
  Paper,
  Progress,
  ScrollArea,
  Stack,
  Table,
  Text,
  TextInput,
  ThemeIcon,
  Tooltip,
} from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { mdiHeartPulse, mdiMagnify } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import useSWR from 'swr'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { useLanguage } from '@Utils/I18n'
import tableClasses from '@Styles/Table.module.css'

interface ChallengeHealthModel {
  id: number
  title: string
  category: string
  solveCount: number
  wrongCount: number
  totalAttempts: number
  wrongRate: number
  firstSolveTime: string | null
  solveTeamCount: number
}

const fetcher = (url: string) =>
  fetch(url, { credentials: 'include' }).then((r) => {
    if (!r.ok) throw new Error('Failed to fetch')
    return r.json()
  })

const CATEGORY_COLORS: Record<string, string> = {
  Misc: 'gray',
  Crypto: 'violet',
  Pwn: 'red',
  Web: 'blue',
  Reverse: 'orange',
  Blockchain: 'teal',
  Forensics: 'green',
  Hardware: 'cyan',
  Mobile: 'pink',
  PPC: 'lime',
}

const Health: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1', 10)
  const { t } = useTranslation()
  const { locale } = useLanguage()
  const [search, setSearch] = useState('')
  const [debounced] = useDebouncedValue(search, 300)

  const { data: health, isLoading } = useSWR<ChallengeHealthModel[]>(
    `/api/admin/Games/${numId}/Challenges/Health`,
    fetcher,
    { refreshInterval: 30000 }
  )

  const filtered = health?.filter(
    (c) =>
      debounced === '' ||
      c.title.toLowerCase().includes(debounced.toLowerCase()) ||
      c.category.toLowerCase().includes(debounced.toLowerCase())
  ) ?? []

  const totalSolves = health?.reduce((s, c) => s + c.solveCount, 0) ?? 0
  const totalWrong = health?.reduce((s, c) => s + c.wrongCount, 0) ?? 0
  const solvedChallenges = health?.filter((c) => c.solveCount > 0).length ?? 0

  return (
    <WithGameEditTab
      isLoading={isLoading}
      head={
        <Group justify="space-between" w="100%">
          <TextInput
            w="36%"
            size="sm"
            leftSection={<Icon path={mdiMagnify} size={0.9} />}
            placeholder={t('admin.placeholder.health.search', 'Filter by title or category…')}
            value={search}
            onChange={(e) => setSearch(e.currentTarget.value)}
          />
          <Group gap="xl">
            <Stack gap={0} align="center">
              <Text fw={700} size="lg" c="teal">{solvedChallenges}/{health?.length ?? 0}</Text>
              <Text size="xs" c="dimmed">{t('admin.label.health.solved_challenges', 'Challenges Solved')}</Text>
            </Stack>
            <Stack gap={0} align="center">
              <Text fw={700} size="lg" c="blue">{totalSolves}</Text>
              <Text size="xs" c="dimmed">{t('admin.label.health.total_solves', 'Total Solves')}</Text>
            </Stack>
            <Stack gap={0} align="center">
              <Text fw={700} size="lg" c="orange">{totalWrong}</Text>
              <Text size="xs" c="dimmed">{t('admin.label.health.total_wrong', 'Wrong Attempts')}</Text>
            </Stack>
          </Group>
        </Group>
      }
    >
      {isLoading ? (
        <Center h="60vh">
          <Loader />
        </Center>
      ) : (
        <Paper shadow="md" p="xs" w="100%">
          <ScrollArea offsetScrollbars scrollbarSize={4} h="calc(100vh - 220px)">
            <Table className={tableClasses.table} highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>{t('admin.label.health.challenge', 'Challenge')}</Table.Th>
                  <Table.Th>{t('admin.label.health.category', 'Category')}</Table.Th>
                  <Table.Th miw={80}>{t('admin.label.health.teams_solved', 'Teams')}</Table.Th>
                  <Table.Th miw={80}>{t('admin.label.health.solve_count', 'Solves')}</Table.Th>
                  <Table.Th miw={80}>{t('admin.label.health.wrong_count', 'Wrong')}</Table.Th>
                  <Table.Th miw={160}>{t('admin.label.health.wrong_rate', 'Wrong Rate')}</Table.Th>
                  <Table.Th>{t('admin.label.health.first_solve', 'First Solve')}</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {filtered.map((c) => (
                  <Table.Tr key={c.id}>
                    <Table.Td>
                      <Group gap="xs">
                        {c.solveCount > 0 ? (
                          <ThemeIcon size="xs" color="teal" variant="light" radius="xl">
                            <Icon path={mdiHeartPulse} size={0.6} />
                          </ThemeIcon>
                        ) : (
                          <ThemeIcon size="xs" color="gray" variant="light" radius="xl">
                            <Icon path={mdiHeartPulse} size={0.6} />
                          </ThemeIcon>
                        )}
                        <Text size="sm" fw={500}>{c.title}</Text>
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Badge size="sm" color={CATEGORY_COLORS[c.category] ?? 'gray'} variant="light">
                        {c.category}
                      </Badge>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" ff="monospace">{c.solveTeamCount}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" ff="monospace" c="teal">{c.solveCount}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" ff="monospace" c={c.wrongCount > 20 ? 'red' : undefined}>{c.wrongCount}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Tooltip label={`${c.wrongRate}%`} position="right" withArrow>
                        <Stack gap={2}>
                          <Progress
                            value={c.wrongRate}
                            color={c.wrongRate > 80 ? 'red' : c.wrongRate > 50 ? 'orange' : 'teal'}
                            size="sm"
                            w={140}
                          />
                          <Text size="xs" c="dimmed" ff="monospace">{c.wrongRate}%</Text>
                        </Stack>
                      </Tooltip>
                    </Table.Td>
                    <Table.Td>
                      {c.firstSolveTime ? (
                        <Tooltip label={dayjs(c.firstSolveTime).locale(locale).format('LLL')} withArrow>
                          <Text size="sm" ff="monospace" style={{ cursor: 'help' }}>
                            {dayjs(c.firstSolveTime).locale(locale).fromNow()}
                          </Text>
                        </Tooltip>
                      ) : (
                        <Text size="sm" c="dimmed">—</Text>
                      )}
                    </Table.Td>
                  </Table.Tr>
                ))}
                {filtered.length === 0 && (
                  <Table.Tr>
                    <Table.Td colSpan={7}>
                      <Text ta="center" c="dimmed" py="md" size="sm">
                        {debounced ? t('admin.placeholder.health.no_match', 'No challenges match the filter.') : t('admin.placeholder.health.empty', 'No challenges found.')}
                      </Text>
                    </Table.Td>
                  </Table.Tr>
                )}
              </Table.Tbody>
            </Table>
          </ScrollArea>
        </Paper>
      )}
    </WithGameEditTab>
  )
}

export default Health

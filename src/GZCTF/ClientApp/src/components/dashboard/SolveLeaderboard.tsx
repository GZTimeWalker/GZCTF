import { Table } from '@mantine/core'
import type { DashboardSnapshot } from '@Hooks/useDashboard'
import { useTranslation } from 'react-i18next'

export const SolveLeaderboard = ({ snapshot }: { snapshot: DashboardSnapshot }) => {
  const { t } = useTranslation('learning')
  return <Table striped withTableBorder>
    <Table.Thead><Table.Tr><Table.Th>{t('dashboardRank')}</Table.Th><Table.Th>{t('dashboardUsername')}</Table.Th><Table.Th>{t('dashboardSolved')}</Table.Th></Table.Tr></Table.Thead>
    <Table.Tbody>{snapshot.leaderboard.map((row) => <Table.Tr key={row.userName}><Table.Td>{row.rank}</Table.Td><Table.Td>{row.userName}</Table.Td><Table.Td>{row.uniqueSolvedCount}</Table.Td></Table.Tr>)}</Table.Tbody>
  </Table>
}

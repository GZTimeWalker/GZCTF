import { Table } from '@mantine/core'
import type { DashboardSnapshot } from '@Hooks/useDashboard'

export const SolveLeaderboard = ({ snapshot }: { snapshot: DashboardSnapshot }) => (
  <Table striped withTableBorder>
    <Table.Thead><Table.Tr><Table.Th>Rank</Table.Th><Table.Th>Username</Table.Th><Table.Th>Solved</Table.Th></Table.Tr></Table.Thead>
    <Table.Tbody>{snapshot.leaderboard.map((row) => <Table.Tr key={row.userName}><Table.Td>{row.rank}</Table.Td><Table.Td>{row.userName}</Table.Td><Table.Td>{row.uniqueSolvedCount}</Table.Td></Table.Tr>)}</Table.Tbody>
  </Table>
)


import { Button, Card, FileInput, Group, Stack, Table, Text, Title } from '@mantine/core'
import { useState } from 'react'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useImportBatches, adminRequest } from '@Hooks/useAdminLearning'
import { Role } from '@Api'

const AdminImports = () => {
  const { data: batches, mutate } = useImportBatches()
  const [file, setFile] = useState<File | null>(null)
  const upload = async () => {
    if (!file) return
    const body = new FormData()
    body.append('package', file)
    await adminRequest('/api/admin/imports/zip', { method: 'POST', body })
    setFile(null)
    await mutate()
  }
  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack><Title order={1}>Import review</Title><Card withBorder><Group><FileInput value={file} onChange={setFile} accept=".zip" placeholder="Legacy game ZIP" /><Button onClick={upload} disabled={!file}>Upload</Button></Group></Card><Table><Table.Thead><Table.Tr><Table.Th>Batch</Table.Th><Table.Th>Status</Table.Th><Table.Th>Challenges</Table.Th><Table.Th>Routes</Table.Th><Table.Th>Warnings</Table.Th></Table.Tr></Table.Thead><Table.Tbody>{batches?.map((batch) => <Table.Tr key={batch.id}><Table.Td>{batch.id}</Table.Td><Table.Td>{batch.state}</Table.Td><Table.Td>{batch.challengeCount}</Table.Td><Table.Td>{batch.pathCount}</Table.Td><Table.Td>{batch.warningCount}</Table.Td></Table.Tr>)}</Table.Tbody></Table>{batches?.map((batch) => batch.parityReportJson && <Text key={`${batch.id}-report`} size="sm">{batch.parityReportJson}</Text>)}</Stack></WithNavBar></WithRole>
}

export default AdminImports


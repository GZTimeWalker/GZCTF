import { Button, Card, FileInput, Group, Stack, Table, Text, Title } from '@mantine/core'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { WithNavBar } from '@Components/WithNavbar'
import { WithRole } from '@Components/WithRole'
import { useImportBatches } from '@Hooks/useChallengeLibraryAdmin'
import api, { Role } from '@Api'
import { showErrorMsg } from '@Utils/Shared'

const AdminImports = () => {
  const { data: batches, mutate } = useImportBatches()
  const { t } = useTranslation()
  const [file, setFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const upload = async () => {
    if (!file) return
    setUploading(true)
    try {
      await api.imports.importsUpload({ package: file })
      setFile(null)
      await mutate()
    } catch (error) { showErrorMsg(error, t) } finally { setUploading(false) }
  }
  return <WithRole requiredRole={Role.Admin}><WithNavBar minWidth={0}><Stack><Title order={1}>{t('learning:adminImportReview')}</Title><Card withBorder><Group><FileInput value={file} onChange={setFile} accept=".zip" placeholder={t('learning:adminLegacyZip')} /><Button loading={uploading} onClick={upload} disabled={!file}>{t('learning:adminUpload')}</Button></Group></Card><Table><Table.Thead><Table.Tr><Table.Th>{t('learning:adminBatch')}</Table.Th><Table.Th>{t('learning:adminStatus')}</Table.Th><Table.Th>{t('learning:adminChallenges')}</Table.Th><Table.Th>{t('learning:adminRoutes')}</Table.Th><Table.Th>{t('learning:adminWarnings')}</Table.Th></Table.Tr></Table.Thead><Table.Tbody>{batches?.map((batch) => <Table.Tr key={batch.id}><Table.Td>{batch.id}</Table.Td><Table.Td>{batch.state}</Table.Td><Table.Td>{batch.challengeCount}</Table.Td><Table.Td>{batch.pathCount}</Table.Td><Table.Td>{batch.warningCount}</Table.Td></Table.Tr>)}</Table.Tbody></Table>{batches?.map((batch) => batch.parityReportJson && <Text key={`${batch.id}-report`} size="sm">{batch.parityReportJson}</Text>)}</Stack></WithNavBar></WithRole>
}

export default AdminImports

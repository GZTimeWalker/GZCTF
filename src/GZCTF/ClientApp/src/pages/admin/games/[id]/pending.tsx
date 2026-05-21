import {
  Badge,
  Button,
  Center,
  Group,
  Stack,
  Table,
  Text,
  Textarea,
  Title,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiDeleteOutline, mdiMagnify } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import { FC, useState } from 'react'

dayjs.extend(relativeTime)
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { ChallengeAuditModal } from '@Components/admin/ChallengeAuditModal'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import api, { PendingChallengeModel } from '@Api'

const PendingChallenges: FC = () => {
  const { id } = useParams()
  const gameId = parseInt(id ?? '-1')
  const { t } = useTranslation()
  const modals = useModals()
  const [busy, setBusy] = useState(false)

  const { data: pending, mutate } = api.edit.useEditListPendingChallenges(gameId, undefined, gameId > 0)

  const [auditTarget, setAuditTarget] = useState<{ id: number; title: string; submitter?: string | null } | null>(null)

  const onApprove = async (cId: number) => {
    setBusy(true)
    try {
      await api.edit.editApproveChallenge(gameId, cId)
      showNotification({ color: 'teal', message: t('admin.notification.review.approved'), icon: <Icon path={mdiCheck} size={1} /> })
      mutate()
    } catch (e) {
      showErrorMsg(e, t)
    } finally {
      setBusy(false)
    }
  }

  const onReject = (cId: number, title: string) => {
    let note = ''
    modals.openConfirmModal({
      title: t('admin.content.review.reject_title', { name: title }),
      children: (
        <Textarea
          placeholder={t('admin.content.review.reject_note_placeholder')}
          onChange={(e) => (note = e.currentTarget.value)}
          minRows={3}
          autosize
        />
      ),
      labels: { confirm: t('admin.button.review.reject'), cancel: t('common.button.cancel') },
      confirmProps: { color: 'red' },
      onConfirm: async () => {
        setBusy(true)
        try {
          await api.edit.editRejectChallenge(gameId, cId, { note })
          showNotification({ color: 'teal', message: t('admin.notification.review.rejected'), icon: <Icon path={mdiClose} size={1} /> })
          mutate()
        } catch (e) {
          showErrorMsg(e, t)
        } finally {
          setBusy(false)
        }
      },
    })
  }

  const onDelete = (row: PendingChallengeModel) => {
    modals.openConfirmModal({
      title: t('admin.content.review.delete_title', { name: row.title }),
      children: <Text size="sm">{t('admin.content.review.delete_warning')}</Text>,
      labels: { confirm: t('admin.button.review.delete'), cancel: t('common.button.cancel') },
      confirmProps: { color: 'red' },
      onConfirm: async () => {
        setBusy(true)
        try {
          await api.edit.editRemoveGameChallenge(gameId, row.id)
          showNotification({
            color: 'teal',
            message: t('admin.notification.review.deleted', { name: row.title }),
            icon: <Icon path={mdiDeleteOutline} size={1} />,
          })
          mutate()
        } catch (e) {
          showErrorMsg(e, t)
        } finally {
          setBusy(false)
        }
      },
    })
  }

  return (
    <WithGameEditTab isLoading={!pending}>
      <Stack gap="md" w="100%">
        <Title order={3}>{t('admin.content.review.title')}</Title>
        {!pending || pending.length === 0 ? (
          <Center h="40vh">
            <Stack gap={0} align="center">
              <Title order={4}>{t('admin.content.review.empty.title')}</Title>
              <Text c="dimmed">{t('admin.content.review.empty.description')}</Text>
            </Stack>
          </Center>
        ) : (
          <Table striped highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>{t('admin.content.review.column.status')}</Table.Th>
                <Table.Th>{t('admin.content.review.column.submitted')}</Table.Th>
                <Table.Th>{t('admin.content.review.column.submitter')}</Table.Th>
                <Table.Th>{t('admin.content.review.column.title')}</Table.Th>
                <Table.Th>{t('admin.content.review.column.category')}</Table.Th>
                <Table.Th>{t('admin.content.review.column.type')}</Table.Th>
                <Table.Th>{t('admin.content.review.column.actions')}</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {pending.map((row) => {
                const isRejected = row.reviewStatus === 'Rejected'
                return (
                  <Table.Tr key={row.id}>
                    <Table.Td>
                      <Badge size="sm" color={isRejected ? 'red' : 'yellow'} variant="filled">
                        {isRejected
                          ? t('admin.content.review.badge.rejected')
                          : t('admin.content.review.badge.pending')}
                      </Badge>
                    </Table.Td>
                    <Table.Td>{row.submittedAtUtc ? dayjs(row.submittedAtUtc).fromNow() : '—'}</Table.Td>
                    <Table.Td>{row.submittedByUserName ?? '—'}</Table.Td>
                    <Table.Td>
                      <Stack gap={0}>
                        <Text fw="bold">{row.title}</Text>
                        {row.reviewNote && (
                          <Text size="xs" c="dimmed" lineClamp={2}>
                            {row.reviewNote}
                          </Text>
                        )}
                      </Stack>
                    </Table.Td>
                    <Table.Td><Badge variant="light">{row.category}</Badge></Table.Td>
                    <Table.Td><Badge variant="outline">{row.type}</Badge></Table.Td>
                    <Table.Td>
                      <Group gap="xs" wrap="nowrap">
                        <Button
                          size="xs"
                          variant="default"
                          leftSection={<Icon path={mdiMagnify} size={0.8} />}
                          onClick={() => setAuditTarget({ id: row.id, title: row.title, submitter: row.submittedByUserName })}
                        >
                          {t('admin.button.review.audit')}
                        </Button>
                        <Button size="xs" color="teal" disabled={busy} onClick={() => onApprove(row.id)}>
                          {t('admin.button.review.approve')}
                        </Button>
                        {!isRejected && (
                          <Button size="xs" color="red" variant="outline" disabled={busy} onClick={() => onReject(row.id, row.title)}>
                            {t('admin.button.review.reject')}
                          </Button>
                        )}
                        <Button
                          size="xs"
                          color="red"
                          variant="subtle"
                          leftSection={<Icon path={mdiDeleteOutline} size={0.8} />}
                          disabled={busy}
                          onClick={() => onDelete(row)}
                        >
                          {t('admin.button.review.delete')}
                        </Button>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                )
              })}
            </Table.Tbody>
          </Table>
        )}
      </Stack>
      <ChallengeAuditModal
        gameId={gameId}
        challengeId={auditTarget?.id ?? null}
        challengeTitle={auditTarget?.title}
        submitter={auditTarget?.submitter}
        opened={auditTarget != null}
        onClose={() => setAuditTarget(null)}
      />
    </WithGameEditTab>
  )
}

export default PendingChallenges

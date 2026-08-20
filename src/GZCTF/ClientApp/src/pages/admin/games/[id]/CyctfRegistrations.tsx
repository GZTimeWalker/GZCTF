import {
  Badge,
  Button,
  Group,
  Modal,
  Paper,
  Stack,
  Table,
  Text,
  Textarea,
  Title,
} from '@mantine/core'
import { useDisclosure, useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import { useAdminGame } from '@Hooks/useGame'
import api from '@Api'
import type { RegistrationResponse } from '@Api'

const CyctfRegistrations: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { game } = useAdminGame(numId)
  const { t } = useTranslation()

  const [registrations, setRegistrations] = useState<RegistrationResponse[]>([])
  const [stats, setStats] = useState<Record<string, number>>({})
  const [selectedReg, setSelectedReg] = useState<RegistrationResponse | null>(null)
  const [reviewNote, setReviewNote] = useInputState('')
  const [opened, { open, close }] = useDisclosure(false)

  useEffect(() => {
    if (numId > 0) {
      loadData()
    }
  }, [numId])

  const loadData = async () => {
    try {
      const [regsRes, statsRes] = await Promise.all([
        api.registration.registrationGetGameRegistrations(numId),
        api.registration.registrationGetRegistrationStats(numId),
      ])
      setRegistrations(regsRes)
      setStats(statsRes)
    } catch (err) {
      showErrorMsg(err, t)
    }
  }

  const getStatusBadge = (status: string) => {
    const colors: Record<string, string> = {
      PENDING: 'yellow',
      APPROVED: 'green',
      REJECTED: 'red',
      CANCELLED: 'gray',
    }
    return (
      <Badge color={colors[status] || 'gray'} variant="light">
        {status}
      </Badge>
    )
  }

  const openReviewModal = (reg: RegistrationResponse) => {
    setSelectedReg(reg)
    setReviewNote(reg.reviewNote || '')
    open()
  }

  const onReview = async (status: string) => {
    if (!selectedReg) return

    try {
      await api.registration.registrationReviewRegistration(selectedReg.id, {
        status,
        reviewNote: reviewNote.trim() || undefined,
      })
      showNotification({
        color: 'teal',
        message: '审核成功',
        icon: <Icon path={mdiCheck} size={1} />,
      })
      close()
      loadData()
    } catch (err) {
      showErrorMsg(err, t)
    }
  }

  return (
    <WithGameEditTab>
      <Stack gap="md">
        <Group justify="space-between" align="center">
          <Title order={3}>报名管理</Title>
          <Group gap="md">
            <Text size="sm">
              待审核: <strong>{stats.PENDING || 0}</strong>
            </Text>
            <Text size="sm">
              已通过: <strong>{stats.APPROVED || 0}</strong>
            </Text>
            <Text size="sm">
              已拒绝: <strong>{stats.REJECTED || 0}</strong>
            </Text>
          </Group>
        </Group>

        <Paper shadow="sm" p="md">
          <Table striped highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>队伍</Table.Th>
                <Table.Th>组别</Table.Th>
                <Table.Th>状态</Table.Th>
                <Table.Th>报名时间</Table.Th>
                <Table.Th>审核人</Table.Th>
                <Table.Th>操作</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {registrations.map((reg) => (
                <Table.Tr key={reg.id}>
                  <Table.Td>{reg.teamName}</Table.Td>
                  <Table.Td>{reg.divisionName}</Table.Td>
                  <Table.Td>{getStatusBadge(reg.status)}</Table.Td>
                  <Table.Td>{dayjs(reg.createTime).format('YYYY-MM-DD HH:mm')}</Table.Td>
                  <Table.Td>{reg.reviewedBy || '-'}</Table.Td>
                  <Table.Td>
                    <Button size="xs" onClick={() => openReviewModal(reg)}>
                      审核
                    </Button>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>

          {registrations.length === 0 && (
            <Text ta="center" c="dimmed" py="xl">
              暂无报名记录
            </Text>
          )}
        </Paper>
      </Stack>

      <Modal opened={opened} onClose={close} title="审核报名" size="lg">
        {selectedReg && (
          <Stack gap="md">
            <Group>
              <Text fw={500}>队伍:</Text>
              <Text>{selectedReg.teamName}</Text>
            </Group>
            <Group>
              <Text fw={500}>组别:</Text>
              <Text>{selectedReg.divisionName}</Text>
            </Group>
            <Group>
              <Text fw={500}>当前状态:</Text>
              {getStatusBadge(selectedReg.status)}
            </Group>

            {selectedReg.formData && (
              <Stack gap="xs">
                <Text fw={500}>报名表单数据:</Text>
                <Paper p="sm" withBorder>
                  <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                    {selectedReg.formData}
                  </Text>
                </Paper>
              </Stack>
            )}

            <Textarea
              label="审核备注"
              value={reviewNote}
              onChange={setReviewNote}
              minRows={3}
              placeholder="输入审核意见..."
            />

            <Group justify="flex-end" gap="sm">
              <Button variant="outline" onClick={close}>
                取消
              </Button>
              <Button
                color="red"
                onClick={() => onReview('REJECTED')}
                leftSection={<Icon path={mdiClose} size={0.8} />}
              >
                拒绝
              </Button>
              <Button
                color="green"
                onClick={() => onReview('APPROVED')}
                leftSection={<Icon path={mdiCheck} size={0.8} />}
              >
                通过
              </Button>
            </Group>
          </Stack>
        )}
      </Modal>
    </WithGameEditTab>
  )
}

export default CyctfRegistrations

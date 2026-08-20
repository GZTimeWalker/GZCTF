import {
  ActionIcon,
  Button,
  ColorInput,
  Group,
  Modal,
  MultiSelect,
  NumberInput,
  Paper,
  Stack,
  Table,
  Text,
  Textarea,
  TextInput,
  Title,
} from '@mantine/core'
import { useDisclosure, useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiDeleteOutline, mdiPencilOutline, mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import { useAdminGame } from '@Hooks/useGame'
import api from '@Api'
import type { AwardRequest, AwardResponse } from '@Api'

const CyctfAwards: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { game } = useAdminGame(numId)
  const { t } = useTranslation()

  const [awards, setAwards] = useState<AwardResponse[]>([])
  const [editingAward, setEditingAward] = useState<AwardResponse | null>(null)
  const [opened, { open, close }] = useDisclosure(false)
  const [teams, setTeams] = useState<{ value: string; label: string }[]>([])

  const [name, setName] = useInputState('')
  const [description, setDescription] = useInputState('')
  const [primaryColor, setPrimaryColor] = useInputState('#3B6DFF')
  const [secondaryColor, setSecondaryColor] = useInputState('#6EE7B7')
  const [sortOrder, setSortOrder] = useState(0)
  const [selectedTeams, setSelectedTeams] = useState<string[]>([])

  useEffect(() => {
    if (numId > 0) {
      loadData()
    }
  }, [numId])

  const loadData = async () => {
    try {
      const [awardsRes, teamsRes] = await Promise.all([
        api.award.awardGetAwards(numId),
        api.game.gameParticipations(numId, {}),
      ])
      setAwards(awardsRes)

      // 转换队伍数据为 MultiSelect 格式
      const teamOptions = teamsRes.data.map((p: any) => ({
        value: p.team.id.toString(),
        label: p.team.name || `Team ${p.team.id}`,
      }))
      setTeams(teamOptions)
    } catch (err) {
      showErrorMsg(err, t)
    }
  }

  const openModal = (award?: AwardResponse) => {
    if (award) {
      setEditingAward(award)
      setName(award.name)
      setDescription(award.description || '')
      setPrimaryColor(award.primaryColor || '#3B6DFF')
      setSecondaryColor(award.secondaryColor || '#6EE7B7')
      setSortOrder(award.sortOrder)
      setSelectedTeams(award.teamIds?.map((id) => id.toString()) || [])
    } else {
      setEditingAward(null)
      setName('')
      setDescription('')
      setPrimaryColor('#3B6DFF')
      setSecondaryColor('#6EE7B7')
      setSortOrder(0)
      setSelectedTeams([])
    }
    open()
  }

  const onSave = async () => {
    if (!name.trim()) {
      showNotification({
        color: 'red',
        message: '请填写奖项名称',
        icon: <Icon path={mdiCheck} size={1} />,
      })
      return
    }

    const data: AwardRequest = {
      name: name.trim(),
      description: description.trim() || undefined,
      primaryColor: primaryColor || undefined,
      secondaryColor: secondaryColor || undefined,
      sortOrder,
    }

    try {
      if (editingAward) {
        await api.award.awardUpdateAward(numId, editingAward.id, data)
      } else {
        await api.award.awardCreateAward(numId, data)
      }
      showNotification({
        color: 'teal',
        message: '保存成功',
        icon: <Icon path={mdiCheck} size={1} />,
      })
      close()
      loadData()
    } catch (err) {
      showErrorMsg(err, t)
    }
  }

  const onDelete = async (award: AwardResponse) => {
    if (!confirm(`确定要删除奖项 "${award.name}" 吗？`)) return

    try {
      await api.award.awardDeleteAward(numId, award.id)
      showNotification({
        color: 'teal',
        message: '删除成功',
        icon: <Icon path={mdiCheck} size={1} />,
      })
      loadData()
    } catch (err) {
      showErrorMsg(err, t)
    }
  }

  return (
    <WithGameEditTab>
      <Stack gap="md">
        <Group justify="space-between" align="center">
          <Title order={3}>奖项管理</Title>
          <Button
            leftSection={<Icon path={mdiPlus} size={1} />}
            onClick={() => openModal()}
          >
            添加奖项
          </Button>
        </Group>

        <Paper shadow="sm" p="md">
          <Table striped highlightOnHover>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>奖项名称</Table.Th>
                <Table.Th>描述</Table.Th>
                <Table.Th>获奖队伍</Table.Th>
                <Table.Th>排序</Table.Th>
                <Table.Th>操作</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {awards.map((award) => (
                <Table.Tr key={award.id}>
                  <Table.Td>
                    <Group gap="xs">
                      <div
                        style={{
                          width: 12,
                          height: 12,
                          borderRadius: '50%',
                          background: award.primaryColor || '#3B6DFF',
                        }}
                      />
                      {award.name}
                    </Group>
                  </Table.Td>
                  <Table.Td>
                    {award.description ? (
                      <Text size="sm" lineClamp={1}>
                        {award.description}
                      </Text>
                    ) : (
                      '-'
                    )}
                  </Table.Td>
                  <Table.Td>
                    {award.teamIds && award.teamIds.length > 0
                      ? award.teamIds.join(', ')
                      : '-'}
                  </Table.Td>
                  <Table.Td>{award.sortOrder}</Table.Td>
                  <Table.Td>
                    <Group gap="xs">
                      <ActionIcon
                        variant="light"
                        color="blue"
                        onClick={() => openModal(award)}
                      >
                        <Icon path={mdiPencilOutline} size={0.8} />
                      </ActionIcon>
                      <ActionIcon
                        variant="light"
                        color="red"
                        onClick={() => onDelete(award)}
                      >
                        <Icon path={mdiDeleteOutline} size={0.8} />
                      </ActionIcon>
                    </Group>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>

          {awards.length === 0 && (
            <Text ta="center" c="dimmed" py="xl">
              暂无奖项
            </Text>
          )}
        </Paper>
      </Stack>

      <Modal
        opened={opened}
        onClose={close}
        title={editingAward ? '编辑奖项' : '添加奖项'}
        size="lg"
      >
        <Stack gap="md">
          <TextInput
            label="奖项名称"
            required
            value={name}
            onChange={setName}
            placeholder="一等奖、最佳新人奖等"
          />
          <Textarea
            label="描述"
            value={description}
            onChange={setDescription}
            placeholder="奖项描述"
            minRows={3}
          />
          <Group grow>
            <ColorInput
              label="主颜色"
              value={primaryColor}
              onChange={setPrimaryColor}
            />
            <ColorInput
              label="副颜色"
              value={secondaryColor}
              onChange={setSecondaryColor}
            />
          </Group>
          <NumberInput
            label="排序"
            value={sortOrder}
            onChange={(val) => setSortOrder(Number(val) || 0)}
            min={0}
          />
          <MultiSelect
            label="获奖队伍"
            data={teams}
            value={selectedTeams}
            onChange={setSelectedTeams}
            placeholder="选择获奖队伍"
            searchable
          />

          <Group justify="flex-end" gap="sm">
            <Button variant="outline" onClick={close}>
              取消
            </Button>
            <Button onClick={onSave}>保存</Button>
          </Group>
        </Stack>
      </Modal>
    </WithGameEditTab>
  )
}

export default CyctfAwards

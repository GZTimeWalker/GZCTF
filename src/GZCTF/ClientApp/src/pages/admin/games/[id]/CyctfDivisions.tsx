import {
  Button,
  Group,
  NumberInput,
  Paper,
  Stack,
  Text,
  Textarea,
  Title,
} from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiContentSaveOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import { useAdminGame } from '@Hooks/useGame'
import api from '@Api'
import type { DivisionExtensionRequest, DivisionExtensionResponse } from '@Api'

const CyctfDivisions: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { game } = useAdminGame(numId)
  const { t } = useTranslation()

  const [divisions, setDivisions] = useState<any[]>([])
  const [extensions, setExtensions] = useState<Map<number, DivisionExtensionResponse>>(new Map())
  const [selectedDivision, setSelectedDivision] = useState<number | null>(null)
  const [disabled, setDisabled] = useState(false)

  const [minTeamSize, setMinTeamSize] = useState<number | undefined>(undefined)
  const [maxTeamSize, setMaxTeamSize] = useState<number | undefined>(undefined)
  const [registrationFields, setRegistrationFields] = useInputState('')

  useEffect(() => {
    if (numId > 0) {
      loadData()
    }
  }, [numId])

  const loadData = async () => {
    try {
      const gameRes = await api.edit.editGetGame(numId)
      const divs = gameRes.data.divisions || []
      setDivisions(divs)

      if (divs.length > 0) {
        setSelectedDivision(divs[0].id)
        loadDivisionExtension(divs[0].id)
      }

      // 加载所有组别的扩展配置
      const exts = new Map<number, DivisionExtensionResponse>()
      for (const div of divs) {
        try {
          const res = await api.divisionExtension.divisionExtensionGetDivisionExtension(div.id)
          exts.set(div.id, res.data)
        } catch (err: any) {
          if (err.response?.status !== 404) {
            console.error(`Failed to load extension for division ${div.id}:`, err)
          }
        }
      }
      setExtensions(exts)
    } catch (err) {
      showErrorMsg(err, t)
    }
  }

  const loadDivisionExtension = async (divisionId: number) => {
    try {
      const res = await api.divisionExtension.divisionExtensionGetDivisionExtension(divisionId)
      setMinTeamSize(res.data.minTeamSize ?? undefined)
      setMaxTeamSize(res.data.maxTeamSize ?? undefined)
      setRegistrationFields(res.data.registrationFields || '')
    } catch (err: any) {
      if (err.response?.status === 404) {
        // 没有配置,使用默认值
        setMinTeamSize(undefined)
        setMaxTeamSize(undefined)
        setRegistrationFields('')
      } else {
        showErrorMsg(err, t)
      }
    }
  }

  const onDivisionSelect = (divisionId: number) => {
    setSelectedDivision(divisionId)
    loadDivisionExtension(divisionId)
  }

  const onSave = async () => {
    if (selectedDivision === null) return

    const data: DivisionExtensionRequest = {
      minTeamSize: minTeamSize ?? null,
      maxTeamSize: maxTeamSize ?? null,
      registrationFields: registrationFields.trim() || null,
    }

    setDisabled(true)

    try {
      const res = await api.divisionExtension.divisionExtensionUpdateDivisionExtension(selectedDivision, data)
      const newExtensions = new Map(extensions)
      newExtensions.set(selectedDivision, res.data)
      setExtensions(newExtensions)

      showNotification({
        color: 'teal',
        message: '保存成功',
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (err) {
      showErrorMsg(err, t)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <WithGameEditTab>
      <Stack gap="md">
        <Title order={3}>组别扩展配置</Title>

        <Group align="flex-start" gap="md">
          {/* 左侧：组别列表 */}
          <Paper shadow="sm" p="md" style={{ minWidth: 200 }}>
            <Stack gap="xs">
              <Text fw={500} size="sm">
                选择组别
              </Text>
              {divisions.map((div) => (
                <Button
                  key={div.id}
                  variant={selectedDivision === div.id ? 'filled' : 'light'}
                  onClick={() => onDivisionSelect(div.id)}
                  fullWidth
                >
                  {div.name}
                </Button>
              ))}
              {divisions.length === 0 && (
                <Text size="sm" c="dimmed">
                  暂无组别
                </Text>
              )}
            </Stack>
          </Paper>

          {/* 右侧：配置表单 */}
          <Paper shadow="sm" p="md" style={{ flex: 1 }}>
            {selectedDivision !== null ? (
              <Stack gap="md">
                <Text fw={500}>
                  配置组别:{' '}
                  {divisions.find((d) => d.id === selectedDivision)?.name}
                </Text>

                <Group grow>
                  <NumberInput
                    label="最小队伍人数"
                    description="留空表示不限制"
                    value={minTeamSize}
                    onChange={(val) =>
                      setMinTeamSize(val === '' ? undefined : Number(val))
                    }
                    min={1}
                    disabled={disabled}
                  />
                  <NumberInput
                    label="最大队伍人数"
                    description="留空表示不限制"
                    value={maxTeamSize}
                    onChange={(val) =>
                      setMaxTeamSize(val === '' ? undefined : Number(val))
                    }
                    min={1}
                    disabled={disabled}
                  />
                </Group>

                <Textarea
                  label="自定义报名字段"
                  description="JSON 格式，定义额外的报名表单字段"
                  value={registrationFields}
                  onChange={setRegistrationFields}
                  minRows={6}
                  placeholder='{"fields": [{"name": "school", "label": "学校", "required": true}]}'
                  disabled={disabled}
                  styles={{
                    input: {
                      fontFamily: 'monospace',
                      fontSize: '0.9em',
                    },
                  }}
                />

                <Group justify="flex-end">
                  <Button
                    leftSection={<Icon path={mdiContentSaveOutline} size={1} />}
                    onClick={onSave}
                    disabled={disabled}
                  >
                    保存配置
                  </Button>
                </Group>
              </Stack>
            ) : (
              <Text c="dimmed" ta="center" py="xl">
                请选择一个组别进行配置
              </Text>
            )}
          </Paper>
        </Group>
      </Stack>
    </WithGameEditTab>
  )
}

export default CyctfDivisions

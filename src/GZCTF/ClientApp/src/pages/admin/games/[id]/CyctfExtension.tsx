import {
  Button,
  Group,
  NumberInput,
  Stack,
  Switch,
  Text,
  Textarea,
  Title,
} from '@mantine/core'
import { DateTimePicker } from '@mantine/dates'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiContentSaveOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams } from 'react-router'
import { SwitchLabel } from '@Components/admin/SwitchLabel'
import { WithGameEditTab } from '@Components/admin/WithGameEditTab'
import { showErrorMsg } from '@Utils/Shared'
import { useAdminGame } from '@Hooks/useGame'
import api from '@Api'
import type { GameExtensionRequest, GameExtensionResponse } from '@Api'

const CyctfExtension: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { game } = useAdminGame(numId)
  const { t } = useTranslation()

  const [extension, setExtension] = useState<GameExtensionResponse | null>(null)
  const [disabled, setDisabled] = useState(false)

  const [registrationStart, setRegistrationStart] = useInputState(dayjs())
  const [registrationEnd, setRegistrationEnd] = useInputState(dayjs())
  const [maxTeams, setMaxTeams] = useState<number | undefined>(undefined)
  const [showRegistrationCount, setShowRegistrationCount] = useState(true)
  const [showEventTime, setShowEventTime] = useState(true)
  const [emailWhitelist, setEmailWhitelist] = useInputState('')
  const [status, setStatus] = useInputState('')

  useEffect(() => {
    if (numId > 0) {
      loadExtension()
    }
  }, [numId])

  const loadExtension = async () => {
    try {
      const res = await api.gameExtension.gameExtensionGetGameExtension(numId)
      setExtension(res)
      setRegistrationStart(dayjs(res.registrationStartTime))
      setRegistrationEnd(dayjs(res.registrationEndTime))
      setMaxTeams(res.maxTeams ?? undefined)
      setShowRegistrationCount(res.showRegistrationCount)
      setShowEventTime(res.showEventTime)
      setEmailWhitelist(res.emailWhitelist ?? '')
      setStatus(res.status ?? '')
    } catch (err: any) {
      if (err.response?.status !== 404) {
        showErrorMsg(err, t)
      }
    }
  }

  const onUpdate = async () => {
    if (!game) return

    const data: GameExtensionRequest = {
      registrationStartTime: registrationStart.toISOString(),
      registrationEndTime: registrationEnd.toISOString(),
      maxTeams: maxTeams ?? undefined,
      showRegistrationCount,
      showEventTime,
      emailWhitelist: emailWhitelist.trim() || undefined,
      status: status.trim() || undefined,
    }

    setDisabled(true)

    try {
      const res = await api.gameExtension.gameExtensionUpdateGameExtension(numId, data)
      setExtension(res)
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
        <Title order={3}>CYCTF 扩展配置</Title>

        <Stack gap="sm">
          <Text size="sm" c="dimmed">
            报名时间设置
          </Text>
          <Group grow>
            <DateTimePicker
              label="报名开始时间"
              value={registrationStart.toDate()}
              onChange={(date) => date && setRegistrationStart(dayjs(date))}
              disabled={disabled}
            />
            <DateTimePicker
              label="报名结束时间"
              value={registrationEnd.toDate()}
              onChange={(date) => date && setRegistrationEnd(dayjs(date))}
              disabled={disabled}
            />
          </Group>
        </Stack>

        <NumberInput
          label="最大队伍数量"
          description="留空表示不限制"
          value={maxTeams}
          onChange={(val) => setMaxTeams(val === '' ? undefined : Number(val))}
          min={0}
          disabled={disabled}
        />

        {extension && (
          <Text size="sm">
            当前已报名队伍数: <strong>{extension.currentTeams}</strong>
          </Text>
        )}

        <Switch
          checked={showRegistrationCount}
          onChange={(e) => setShowRegistrationCount(e.currentTarget.checked)}
          label={<SwitchLabel label="显示报名人数" />}
          disabled={disabled}
        />

        <Switch
          checked={showEventTime}
          onChange={(e) => setShowEventTime(e.currentTarget.checked)}
          label={<SwitchLabel label="显示活动时间" />}
          disabled={disabled}
        />

        <Textarea
          label="邮箱白名单"
          description="每行一个邮箱后缀，例如 @example.com"
          value={emailWhitelist}
          onChange={setEmailWhitelist}
          minRows={3}
          disabled={disabled}
        />

        <Textarea
          label="状态文本"
          description="自定义状态显示文本"
          value={status}
          onChange={setStatus}
          disabled={disabled}
        />

        <Group justify="flex-end">
          <Button
            leftSection={<Icon path={mdiContentSaveOutline} size={1} />}
            onClick={onUpdate}
            disabled={disabled}
          >
            保存配置
          </Button>
        </Group>
      </Stack>
    </WithGameEditTab>
  )
}

export default CyctfExtension

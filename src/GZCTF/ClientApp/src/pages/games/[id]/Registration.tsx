import {
  Alert,
  Button,
  Card,
  Container,
  Group,
  Paper,
  Select,
  Stack,
  Text,
  Textarea,
  Title,
} from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiAlertCircle, mdiCheck, mdiInformationOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router'
import { showErrorMsg } from '@Utils/Shared'
import { useGame } from '@Hooks/useGame'
import { useUser } from '@Hooks/useUser'
import cyctfApi, { GameExtensionResponse, RegistrationResponse } from '@/CyctfApi'
import api from '@Api'

const GameRegistration: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const { game } = useGame(numId)
  const { user } = useUser()
  const { t } = useTranslation()
  const navigate = useNavigate()

  const [extension, setExtension] = useState<GameExtensionResponse | null>(null)
  const [registration, setRegistration] = useState<RegistrationResponse | null>(null)
  const [teams, setTeams] = useState<any[]>([])
  const [divisions, setDivisions] = useState<any[]>([])
  const [disabled, setDisabled] = useState(false)

  const [selectedTeam, setSelectedTeam] = useInputState('')
  const [selectedDivision, setSelectedDivision] = useInputState('')
  const [formData, setFormData] = useInputState('')

  useEffect(() => {
    if (numId > 0 && user) {
      loadData()
    }
  }, [numId, user])

  const loadData = async () => {
    try {
      // 加载游戏扩展配置
      const extRes = await cyctfApi.gameExtension.get(numId)
      setExtension(extRes.data)

      // 加载用户的队伍
      const teamsRes = await api.team.teamGetTeamsInfo()
      setTeams(teamsRes.data || [])

      // 加载游戏组别
      if (game?.divisions) {
        setDivisions(game.divisions)
      }

      // 检查是否已报名
      if (teamsRes.data && teamsRes.data.length > 0) {
        const teamId = teamsRes.data[0].id
        try {
          const regRes = await cyctfApi.registration.getByTeamAndGame(numId, teamId!)
          setRegistration(regRes.data)
          setSelectedTeam(teamId!.toString())
          setSelectedDivision(regRes.data.divisionId.toString())
          setFormData(regRes.data.formData || '')
        } catch (err: any) {
          if (err.response?.status !== 404) {
            showErrorMsg(err)
          }
        }
      }
    } catch (err) {
      showErrorMsg(err)
    }
  }

  const onSubmit = async () => {
    if (!selectedTeam || !selectedDivision) {
      showNotification({
        color: 'red',
        message: '请选择队伍和组别',
        icon: <Icon path={mdiAlertCircle} size={1} />,
      })
      return
    }

    setDisabled(true)

    try {
      await cyctfApi.registration.create({
        gameId: numId,
        teamId: parseInt(selectedTeam),
        divisionId: parseInt(selectedDivision),
        formData: formData.trim() || null,
      })

      showNotification({
        color: 'teal',
        message: '报名成功！等待审核',
        icon: <Icon path={mdiCheck} size={1} />,
      })

      loadData()
    } catch (err) {
      showErrorMsg(err)
    } finally {
      setDisabled(false)
    }
  }

  if (!user) {
    return (
      <Container size="md" py="xl">
        <Alert icon={<Icon path={mdiInformationOutline} size={1} />} color="blue">
          请先登录后再进行报名
        </Alert>
      </Container>
    )
  }

  if (!extension) {
    return (
      <Container size="md" py="xl">
        <Alert icon={<Icon path={mdiInformationOutline} size={1} />} color="orange">
          该比赛未开放报名功能
        </Alert>
      </Container>
    )
  }

  const now = dayjs()
  const isBeforeStart = now.isBefore(dayjs(extension.registrationStartTime))
  const isAfterEnd = now.isAfter(dayjs(extension.registrationEndTime))
  const isFull =
    extension.maxTeams !== null && extension.currentTeams >= extension.maxTeams

  const canRegister = !isBeforeStart && !isAfterEnd && !isFull && !registration

  return (
    <Container size="md" py="xl">
      <Stack gap="lg">
        <Title order={2}>比赛报名</Title>

        <Card shadow="sm" padding="lg">
          <Stack gap="md">
            <Group justify="space-between">
              <Text fw={500}>报名时间</Text>
              <Text size="sm" c="dimmed">
                {dayjs(extension.registrationStartTime).format('YYYY-MM-DD HH:mm')} -{' '}
                {dayjs(extension.registrationEndTime).format('YYYY-MM-DD HH:mm')}
              </Text>
            </Group>

            {extension.showRegistrationCount && (
              <Group justify="space-between">
                <Text fw={500}>报名情况</Text>
                <Text size="sm" c="dimmed">
                  {extension.currentTeams}
                  {extension.maxTeams ? ` / ${extension.maxTeams}` : ''} 队
                </Text>
              </Group>
            )}

            {extension.status && (
              <Alert icon={<Icon path={mdiInformationOutline} size={1} />} color="blue">
                {extension.status}
              </Alert>
            )}
          </Stack>
        </Card>

        {isBeforeStart && (
          <Alert icon={<Icon path={mdiAlertCircle} size={1} />} color="orange">
            报名尚未开始
          </Alert>
        )}

        {isAfterEnd && (
          <Alert icon={<Icon path={mdiAlertCircle} size={1} />} color="red">
            报名已结束
          </Alert>
        )}

        {isFull && !registration && (
          <Alert icon={<Icon path={mdiAlertCircle} size={1} />} color="red">
            报名人数已满
          </Alert>
        )}

        {registration ? (
          <Card shadow="sm" padding="lg">
            <Stack gap="md">
              <Group justify="space-between">
                <Title order={4}>您的报名信息</Title>
                {registration.status === 'PENDING' && (
                  <Text c="orange" fw={500}>
                    待审核
                  </Text>
                )}
                {registration.status === 'APPROVED' && (
                  <Text c="green" fw={500}>
                    已通过
                  </Text>
                )}
                {registration.status === 'REJECTED' && (
                  <Text c="red" fw={500}>
                    已拒绝
                  </Text>
                )}
              </Group>

              <Text>
                <strong>队伍:</strong> {registration.teamName}
              </Text>
              <Text>
                <strong>组别:</strong> {registration.divisionName}
              </Text>
              <Text>
                <strong>报名时间:</strong>{' '}
                {dayjs(registration.createTime).format('YYYY-MM-DD HH:mm')}
              </Text>

              {registration.reviewNote && (
                <Alert
                  icon={<Icon path={mdiInformationOutline} size={1} />}
                  color={registration.status === 'REJECTED' ? 'red' : 'blue'}
                >
                  <Text fw={500} mb="xs">
                    审核意见:
                  </Text>
                  {registration.reviewNote}
                </Alert>
              )}
            </Stack>
          </Card>
        ) : (
          canRegister && (
            <Paper shadow="sm" p="lg">
              <Stack gap="md">
                <Select
                  label="选择队伍"
                  required
                  data={teams.map((team) => ({
                    value: team.id!.toString(),
                    label: team.name || `Team ${team.id}`,
                  }))}
                  value={selectedTeam}
                  onChange={setSelectedTeam}
                  disabled={disabled}
                />

                <Select
                  label="选择组别"
                  required
                  data={divisions.map((div) => ({
                    value: div.id!.toString(),
                    label: div.name || `Division ${div.id}`,
                  }))}
                  value={selectedDivision}
                  onChange={setSelectedDivision}
                  disabled={disabled}
                />

                <Textarea
                  label="补充信息"
                  description="可选，填写额外的报名信息"
                  value={formData}
                  onChange={setFormData}
                  minRows={4}
                  disabled={disabled}
                />

                <Button fullWidth onClick={onSubmit} disabled={disabled}>
                  提交报名
                </Button>
              </Stack>
            </Paper>
          )
        )}
      </Stack>
    </Container>
  )
}

export default GameRegistration

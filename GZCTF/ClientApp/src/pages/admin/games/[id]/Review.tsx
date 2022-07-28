import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Accordion,
  ActionIcon,
  Avatar,
  Badge,
  Box,
  Button,
  Group,
  MantineColor,
  Paper,
  Popover,
  Select,
  Stack,
  Text,
  TextProps,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import {
  mdiBackburger,
  mdiCancel,
  mdiCheck,
  mdiClose,
  mdiCrown,
  mdiHelpCircleOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ParticipationInfoModel, ParticipationStatus } from '../../../../Api'
import WithGameTab from '../../../../components/admin/WithGameTab'

const StatusMap = new Map([
  [
    ParticipationStatus.Pending,
    {
      title: '待审核',
      color: 'yellow',
      iconPath: mdiHelpCircleOutline,
      transformTo: [ParticipationStatus.Accepted, ParticipationStatus.Denied],
    },
  ],
  [
    ParticipationStatus.Accepted,
    {
      title: '审核通过',
      color: 'green',
      iconPath: mdiCheck,
      transformTo: [ParticipationStatus.Forfeited],
    },
  ],
  [
    ParticipationStatus.Denied,
    { title: '审核不通过', color: 'red', iconPath: mdiClose, transformTo: [] },
  ],
  [
    ParticipationStatus.Forfeited,
    {
      title: '禁赛',
      color: 'alert',
      iconPath: mdiCancel,
      transformTo: [ParticipationStatus.Accepted],
    },
  ],
])

interface ActionIconWithConfirmProps {
  iconPath: string
  color?: MantineColor
  message: string
  disabled?: boolean
  onClick: () => Promise<any>
}

const ActionIconWithConfirm: FC<ActionIconWithConfirmProps> = (props) => {
  const [opened, setOpened] = useState(false)
  const [loading, setLoading] = useState(false)
  return (
    <Popover
      shadow="md"
      width="max-content"
      withArrow
      position="top"
      opened={opened}
      onChange={setOpened}
    >
      <Popover.Target>
        <ActionIcon
          onClick={() => setOpened(true)}
          disabled={props.disabled && !loading}
          loading={loading}
        >
          <Icon path={props.iconPath} color={props.color} size={1} />
        </ActionIcon>
      </Popover.Target>
      <Popover.Dropdown>
        <Stack align="center">
          <Text size="sm">{props.message}</Text>
          <Group>
            <Button
              size="xs"
              color={props.color}
              disabled={props.disabled && !loading}
              loading={loading}
              onClick={() => {
                setLoading(true)
                props.onClick().finally(() => {
                  setLoading(false)
                  setOpened(false)
                })
              }}
            >
              确定
            </Button>
            <Button
              size="xs"
              variant="outline"
              onClick={() => setOpened(false)}
              disabled={props.disabled}
            >
              取消
            </Button>
          </Group>
        </Stack>
      </Popover.Dropdown>
    </Popover>
  )
}

const GameTeamReview: FC = () => {
  const navigate = useNavigate()
  const theme = useMantineTheme()
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const [disabled, setDisabled] = useState(false)
  const [selectedStatus, setSelectedStatus] = useState<ParticipationStatus | null>(null)
  const [participations, setParticipations] = useState<ParticipationInfoModel[]>()

  const fieldProps: TextProps = {
    size: 'sm',
    sx: { fontFamily: theme.fontFamilyMonospace },
  }

  const setParticipationStatus = async (id: number, status: ParticipationStatus) => {
    setDisabled(true)
    try {
      await api.admin.adminParticipation(id, status)
      setParticipations(
        participations?.map((value) => (value.id === id ? { ...value, status } : value))
      )
      showNotification({
        color: 'teal',
        title: '操作成功',
        message: '参与状态已更新',
        icon: <Icon path={mdiCheck} size={1} />,
        disallowClose: true,
      })
    } catch (err: any) {
      showNotification({
        color: 'red',
        title: '遇到了问题',
        message: `${err.response.data.title}`,
        icon: <Icon path={mdiClose} size={1} />,
      })
    } finally {
      setDisabled(false)
    }
  }

  useEffect(() => {
    if (numId < 0) {
      showNotification({
        color: 'red',
        message: `比赛 Id 错误：${id}`,
        icon: <Icon path={mdiClose} size={1} />,
      })
      navigate('/admin/games')
      return
    }

    api.game.gameParticipations(numId).then((res) => {
      setParticipations(res.data)
    })
  }, [])

  return (
    <WithGameTab
      headProps={{ position: 'left' }}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiBackburger} size={1} />}
            onClick={() => navigate('/admin/games')}
          >
            返回上级
          </Button>
          <Select
            placeholder="全部显示"
            clearable
            data={Array.from(StatusMap, (v) => ({ value: v[0], label: v[1].title }))}
            value={selectedStatus}
            onChange={(value: ParticipationStatus) => setSelectedStatus(value)}
          />
        </>
      }
    >
      <Paper shadow="md">
        <Accordion variant="contained" chevronPosition="left">
          {participations?.map(
            (participation) =>
              (selectedStatus === null || participation.status === selectedStatus) && (
                <Accordion.Item key={participation.id} value={participation.id!.toString()}>
                  <Box sx={{ display: 'flex', alignItems: 'center' }}>
                    <Accordion.Control>
                      <Group>
                        <Avatar src={participation.team?.avatar} />
                        <Box>
                          <Text>
                            {!participation.team?.name ? '（无名队伍）' : participation.team.name}
                          </Text>
                          <Text size="sm" color="dimmed">
                            {participation.team?.bio}
                          </Text>
                        </Box>
                      </Group>
                    </Accordion.Control>
                    <Group p="md" position="apart" sx={{ width: '300px' }}>
                      <Badge color={StatusMap.get(participation.status!)?.color}>
                        {StatusMap.get(participation.status!)?.title}
                      </Badge>
                      <Group>
                        {StatusMap.get(participation.status!)?.transformTo.map((value) => {
                          const s = StatusMap.get(value)!
                          return (
                            <ActionIconWithConfirm
                              key={`${participation.id}@${value}`}
                              iconPath={s.iconPath}
                              color={s.color}
                              message={`确定要设为“${s.title}”吗？`}
                              disabled={disabled}
                              onClick={() => setParticipationStatus(participation.id!, value)}
                            />
                          )
                        })}
                      </Group>
                    </Group>
                  </Box>
                  <Accordion.Panel>
                    {participation.team?.members?.map((user) => (
                      <Group key={user.userId} spacing="xl">
                        <Group>
                          <Avatar src={user.avatar} />
                          <Box>
                            <Group>
                              <Text>{user.userName}</Text>
                              <Text>{!user.realName ? '未填写真实姓名' : user.realName}</Text>
                            </Group>
                            <Text {...fieldProps}>
                              {!user.stdNumber ? '未填写学工号' : user.stdNumber}
                            </Text>
                          </Box>
                        </Group>
                        <Box style={{ width: '1.5rem' }}>
                          {participation.team?.captainId === user.userId && (
                            <Icon path={mdiCrown} size={1} color={theme.colors.yellow[4]} />
                          )}
                        </Box>
                        <Text {...fieldProps}>{!user.email ? '未填写邮箱' : user.email}</Text>
                        <Text {...fieldProps}>{!user.phone ? '未填写手机号码' : user.phone}</Text>
                      </Group>
                    ))}
                  </Accordion.Panel>
                </Accordion.Item>
              )
          )}
        </Accordion>
      </Paper>
    </WithGameTab>
  )
}

export default GameTeamReview

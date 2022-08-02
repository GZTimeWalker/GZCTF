import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Accordion,
  ActionIcon,
  Avatar,
  Badge,
  Box,
  Button,
  Center,
  Group,
  Indicator,
  MantineColor,
  Paper,
  Popover,
  ScrollArea,
  Select,
  Stack,
  Text,
  TextProps,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import {
  mdiAccountOutline,
  mdiBackburger,
  mdiBadgeAccountHorizontalOutline,
  mdiCancel,
  mdiCheck,
  mdiClose,
  mdiCrown,
  mdiEmailOutline,
  mdiHelpCircleOutline,
  mdiPhoneOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import api, {
  ParticipationInfoModel,
  ParticipationStatus,
  ProfileUserInfoModel,
} from '../../../../Api'
import WithGameTab from '../../../../components/admin/WithGameTab'
import { showErrorNotification } from '../../../../utils/ApiErrorHandler'

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
  onClick: () => Promise<void>
}

const ActionIconWithConfirm: FC<ActionIconWithConfirmProps> = (props) => {
  const [opened, setOpened] = useState(false)
  const [loading, setLoading] = useState(false)

  return (
    <Popover shadow="md" width="max-content" position="top" opened={opened} onChange={setOpened}>
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
              disabled={props.disabled}
              onClick={() => setOpened(false)}
            >
              取消
            </Button>
          </Group>
        </Stack>
      </Popover.Dropdown>
    </Popover>
  )
}

interface MemberItemProps {
  user: ProfileUserInfoModel
  isCaptain: boolean
}

const iconProps = {
  size: 0.8,
  color: 'gray',
}

const MemberItem: FC<MemberItemProps> = (props) => {
  const { user, isCaptain } = props
  const theme = useMantineTheme()

  const fieldProps: TextProps = {
    size: 'sm',
    sx: { fontFamily: `${theme.fontFamilyMonospace},${theme.fontFamily}` },
  }

  return (
    <Group spacing="xl">
      {isCaptain ? (
        <Indicator
          inline
          size={16}
          label={<Icon path={mdiCrown} size={0.6} color={theme.colors.yellow[4]} />}
        >
          <Avatar src={user.avatar} />
        </Indicator>
      ) : (
        <Avatar src={user.avatar} />
      )}
      <Box>
        <Group noWrap spacing="xs">
          <Icon path={mdiAccountOutline} {...iconProps} />
          <Group>
            <Text>{user.userName}</Text>
            <Text>{!user.realName ? '未填写真实姓名' : user.realName}</Text>
          </Group>
        </Group>
        <Group noWrap spacing="xs">
          <Icon path={mdiBadgeAccountHorizontalOutline} {...iconProps} />
          <Text {...fieldProps}>{!user.stdNumber ? '未填写' : user.stdNumber}</Text>
        </Group>
      </Box>
      <Box>
        <Group noWrap spacing="xs">
          <Icon path={mdiEmailOutline} {...iconProps} />
          <Text {...fieldProps}>{!user.email ? '未填写' : user.email}</Text>
        </Group>
        <Group noWrap spacing="xs">
          <Icon path={mdiPhoneOutline} {...iconProps} />
          <Text {...fieldProps}>{!user.phone ? '未填写' : user.phone}</Text>
        </Group>
      </Box>
    </Group>
  )
}

interface ParticipationItemProps {
  participation: ParticipationInfoModel
  disabled: boolean
  setParticipationStatus: (id: number, status: ParticipationStatus) => Promise<void>
}

const ParticipationItem: FC<ParticipationItemProps> = (props) => {
  const { participation, disabled, setParticipationStatus } = props

  return (
    <Accordion.Item value={participation.id!.toString()}>
      <Box sx={{ display: 'flex', alignItems: 'center' }}>
        <Accordion.Control>
          <Group>
            <Avatar src={participation.team?.avatar} />
            <Box>
              <Text>{!participation.team?.name ? '（无名队伍）' : participation.team.name}</Text>
              <Text size="sm" color="dimmed">
                {!participation.team?.bio ? '（未设置签名）' : participation.team.bio}
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
          <MemberItem
            key={user.userId}
            user={user}
            isCaptain={participation.team?.captainId === user.userId}
          />
        ))}
      </Accordion.Panel>
    </Accordion.Item>
  )
}

const GameTeamReview: FC = () => {
  const navigate = useNavigate()
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const [disabled, setDisabled] = useState(false)
  const [selectedStatus, setSelectedStatus] = useState<ParticipationStatus | null>(null)
  const [participations, setParticipations] = useState<ParticipationInfoModel[]>()

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
      showErrorNotification(err)
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
      headProps={{ position: 'apart' }}
      isLoading={!participations}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiBackburger} size={1} />}
            onClick={() => navigate('/admin/games')}
          >
            返回上级
          </Button>
          <Group style={{ width: 'calc(100% - 9rem)' }} position="apart">
            <Select
              placeholder="全部显示"
              clearable
              data={Array.from(StatusMap, (v) => ({ value: v[0], label: v[1].title }))}
              value={selectedStatus}
              onChange={(value: ParticipationStatus) => setSelectedStatus(value)}
            />
          </Group>
        </>
      }
    >
      <ScrollArea
        style={{ height: 'calc(100vh - 180px)', position: 'relative' }}
        offsetScrollbars
        type="auto"
      >
        {!participations || participations.length === 0 ? (
          <Center style={{ height: 'calc(100vh - 180px)' }}>
            <Stack spacing={0}>
              <Title order={2}>Ouch! 还没有队伍报名这个比赛</Title>
              <Text>在路上了……别急！</Text>
            </Stack>
          </Center>
        ) : (
          <Paper shadow="md">
            <Accordion variant="contained" chevronPosition="left">
              {participations?.map(
                (participation) =>
                  (selectedStatus === null || participation.status === selectedStatus) && (
                    <ParticipationItem
                      key={participation.id}
                      participation={participation}
                      disabled={disabled}
                      setParticipationStatus={setParticipationStatus}
                    />
                  )
              )}
            </Accordion>
          </Paper>
        )}
      </ScrollArea>
    </WithGameTab>
  )
}

export default GameTeamReview

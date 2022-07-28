import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Accordion,
  Avatar,
  Badge,
  Box,
  Button,
  Group,
  Paper,
  Select,
  Text,
  TextProps,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiBackburger, mdiCheck, mdiClose, mdiCrown } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ParticipationInfoModel, ParticipationStatus } from '../../../../Api'
import WithGameTab from '../../../../components/admin/WithGameTab'

interface LaodingInfo {
  id: number
  status: ParticipationStatus
}

const StatusMap = new Map([
  [ParticipationStatus.Pending, { title: '已报名', color: 'yellow' }],
  [ParticipationStatus.Accepted, { title: '已接受', color: 'green' }],
  [ParticipationStatus.Denied, { title: '已拒绝', color: 'red' }],
  [ParticipationStatus.Forfeited, { title: '已禁赛', color: 'alert' }],
])

const GameTeamReview: FC = () => {
  const navigate = useNavigate()
  const theme = useMantineTheme()
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const [loading, setLoading] = useState<LaodingInfo | null>(null)
  const [selectedStatus, setSelectedStatus] = useState<ParticipationStatus | null>(null)
  const [participations, setParticipations] = useState<ParticipationInfoModel[]>()

  const fieldProps: TextProps = {
    size: 'sm',
    sx: { fontFamily: theme.fontFamilyMonospace },
  }

  const shouldLoading = (participation: ParticipationInfoModel, status: ParticipationStatus) =>
    loading?.id === participation.id && loading?.status === status
  const shouldDisabled = (participation: ParticipationInfoModel, status: ParticipationStatus) =>
    participation.status === status || (!!loading && !shouldLoading(participation, status))

  const setParticipationStatus = (id: number, status: ParticipationStatus) => {
    setLoading({ id, status })
    api.admin
      .adminParticipation(id, status)
      .then(() => {
        setParticipations(
          participations?.map((value) => {
            if (value.id === id) {
              return { ...value, status }
            } else {
              return value
            }
          })
        )
        showNotification({
          color: 'teal',
          title: '操作成功',
          message: '参与状态已更新',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
      .finally(() => {
        setLoading(null)
      })
  }

  const onAccept = (participation: ParticipationInfoModel) =>
    setParticipationStatus(participation.id!, ParticipationStatus.Accepted)
  const onDeny = (participation: ParticipationInfoModel) =>
    setParticipationStatus(participation.id!, ParticipationStatus.Denied)

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
                    <Group noWrap p="md">
                      <Badge color={StatusMap.get(participation.status!)?.color}>
                        {StatusMap.get(participation.status!)?.title}
                      </Badge>
                      <Button
                        leftIcon={<Icon path={mdiCheck} size={1} />}
                        onClick={() => onAccept(participation)}
                        disabled={shouldDisabled(participation, ParticipationStatus.Accepted)}
                        loading={shouldLoading(participation, ParticipationStatus.Accepted)}
                      >
                        接受
                      </Button>
                      <Button
                        leftIcon={<Icon path={mdiClose} size={1} />}
                        color="red"
                        onClick={() => onDeny(participation)}
                        disabled={shouldDisabled(participation, ParticipationStatus.Denied)}
                        loading={shouldLoading(participation, ParticipationStatus.Denied)}
                      >
                        拒绝
                      </Button>
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
                        {participation.team?.captainId === user.userId && (
                          <Icon path={mdiCrown} size={1} color={theme.colors.yellow[4]} />
                        )}
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

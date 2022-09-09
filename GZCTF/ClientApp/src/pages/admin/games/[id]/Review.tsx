import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Accordion,
  Avatar,
  Badge,
  Box,
  Button,
  Center,
  createStyles,
  Group,
  Indicator,
  Paper,
  ScrollArea,
  Select,
  Stack,
  Text,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import {
  mdiAccountOutline,
  mdiKeyboardBackspace,
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
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import WithGameEditTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import api, { ParticipationInfoModel, ParticipationStatus, ProfileUserInfoModel } from '@Api'

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

const useStyles = createStyles((theme) => ({
  root: {
    backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[6] : theme.colors.gray[0],
    borderRadius: theme.radius.sm,
  },

  item: {
    backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[6] : theme.colors.gray[0],
    border: '1px solid rgba(0,0,0,0.2)',
    position: 'relative',
    transition: 'transform 150ms ease',

    '&[data-active]': {
      backgroundColor: theme.colorScheme === 'dark' ? theme.colors.dark[7] : theme.white,
      boxShadow: theme.shadows.md,
    },
  },

  control: {
    padding: '8px 4px',
    ...theme.fn.hover({ background: 'transparent' }),
  },
}))

interface MemberItemProps {
  user: ProfileUserInfoModel
  isRegistered: boolean
  isCaptain: boolean
}

const iconProps = {
  size: 0.8,
  color: 'gray',
}

const MemberItem: FC<MemberItemProps> = (props) => {
  const { user, isCaptain, isRegistered } = props
  const theme = useMantineTheme()

  return (
    <Group spacing="xl" position="apart">
      <Group style={{ width: 'calc(100% - 10rem)' }}>
        {isCaptain ? (
          <Indicator
            inline
            size={16}
            label={<Icon path={mdiCrown} size={0.8} color={theme.colors.yellow[4]} />}
            styles={{
              indicator: {
                backgroundColor: 'transparent',
                marginTop: '-0.8rem',
                marginRight: '1.5rem',
                transform: 'rotate(-30deg)',
              },
            }}
          >
            <Avatar src={user.avatar} />
          </Indicator>
        ) : (
          <Avatar src={user.avatar} />
        )}
        <Group noWrap>
          <Stack spacing={2} style={{ width: '15rem' }}>
            <Group noWrap spacing="xs">
              <Icon path={mdiAccountOutline} {...iconProps} />
              <Group noWrap>
                <Text weight={700}>{user.userName}</Text>
                <Text>{!user.realName ? '' : user.realName}</Text>
              </Group>
            </Group>
            <Group noWrap spacing="xs">
              <Icon path={mdiBadgeAccountHorizontalOutline} {...iconProps} />
              <Text>{!user.stdNumber ? '未填写' : user.stdNumber}</Text>
            </Group>
          </Stack>
          <Stack spacing={2}>
            <Group noWrap spacing="xs">
              <Icon path={mdiEmailOutline} {...iconProps} />
              <Text>{!user.email ? '未填写' : user.email}</Text>
            </Group>
            <Group noWrap spacing="xs">
              <Icon path={mdiPhoneOutline} {...iconProps} />
              <Text>{!user.phone ? '未填写' : user.phone}</Text>
            </Group>
          </Stack>
        </Group>
      </Group>
      <Text size="sm" weight={500} color={isRegistered ? 'teal' : 'yellow'}>
        {isRegistered ? '已报名' : '未报名'}
      </Text>
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
  const theme = useMantineTheme()

  return (
    <Accordion.Item value={participation.id!.toString()}>
      <Box sx={{ display: 'flex', alignItems: 'center' }}>
        <Accordion.Control>
          <Group position="apart">
            <Group>
              <Avatar src={participation.team?.avatar} />
              <Box>
                <Text weight={500}>
                  {!participation.team?.name ? '（无名队伍）' : participation.team.name}
                </Text>
                <Text size="sm" color="dimmed">
                  {!participation.team?.bio ? '（未设置签名）' : participation.team.bio}
                </Text>
              </Box>
            </Group>
            <Group position="apart" style={{ width: 'calc(30%)' }}>
              <Box>
                <Text>{participation.organization}</Text>
                <Text size="sm" color="dimmed" weight={700}>
                  {participation.registeredMembers?.length ?? 0}/
                  {participation.team?.members?.length ?? 0} 已报名
                </Text>
              </Box>
              <Badge color={StatusMap.get(participation.status!)?.color}>
                {StatusMap.get(participation.status!)?.title}
              </Badge>
            </Group>
          </Group>
        </Accordion.Control>
        <Group
          style={{ margin: `0 ${theme.spacing.xl}px`, minWidth: `${theme.spacing.xl * 3}px` }}
          position="right"
        >
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
      </Box>
      <Accordion.Panel>
        {participation.team?.members?.map((user) => (
          <MemberItem
            key={user.userId}
            user={user}
            isRegistered={participation.registeredMembers?.some((u) => u === user.userId) ?? false}
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
  const { classes } = useStyles()

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
        disallowClose: true,
      })
      navigate('/admin/games')
      return
    }

    api.game.gameParticipations(numId).then((res) => {
      setParticipations(res.data)
    })
  }, [])

  return (
    <WithGameEditTab
      headProps={{ position: 'apart' }}
      isLoading={!participations}
      head={
        <>
          <Button
            leftIcon={<Icon path={mdiKeyboardBackspace} size={1} />}
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
          <Center style={{ height: 'calc(100vh - 200px)' }}>
            <Stack spacing={0}>
              <Title order={2}>Ouch! 还没有队伍报名这个比赛</Title>
              <Text>在路上了……别急！</Text>
            </Stack>
          </Center>
        ) : (
          <Paper shadow="md">
            <Accordion
              variant="contained"
              chevronPosition="left"
              classNames={classes}
              className={classes.root}
            >
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
    </WithGameEditTab>
  )
}

export default GameTeamReview

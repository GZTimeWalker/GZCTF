import { FC, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Accordion,
  Avatar,
  Badge,
  Box,
  Button,
  Center,
  Group,
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
  mdiCheck,
  mdiClose,
  mdiEmailOutline,
  mdiPhoneOutline,
  mdiStar,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { ParticipationStatusControl } from '@Components/admin/ParticipationStatusControl'
import WithGameEditTab from '@Components/admin/WithGameEditTab'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { ParticipationStatusMap } from '@Utils/Shared'
import { useAccordionStyles } from '@Utils/ThemeOverride'
import api, { ParticipationInfoModel, ParticipationStatus, ProfileUserInfoModel } from '@Api'

interface MemberItemProps {
  user: ProfileUserInfoModel
  isRegistered: boolean
  isCaptain: boolean
}

const iconProps = {
  size: 0.9,
  color: 'gray',
}

const MemberItem: FC<MemberItemProps> = (props) => {
  const { user, isCaptain, isRegistered } = props
  const theme = useMantineTheme()

  return (
    <Group spacing="xl" position="apart">
      <Group w="calc(100% - 10rem)">
        <Avatar alt="avatar" src={user.avatar}>
          {user.userName?.slice(0, 1) ?? 'U'}
        </Avatar>
        <Group noWrap>
          <Stack spacing={2} w="15rem">
            <Group noWrap spacing="xs">
              <Icon path={mdiAccountOutline} {...iconProps} />
              <Group noWrap>
                <Text fw={700}>{user.userName}</Text>
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
      <Group position="right">
        {isCaptain && (
          <Group spacing={0}>
            <Icon path={mdiStar} color={theme.colors.yellow[4]} size={0.9} />
            <Text size="sm" fw={500} c="yellow">
              队长
            </Text>
          </Group>
        )}
        <Text size="sm" fw={700} c={isRegistered ? 'teal' : 'orange'}>
          {isRegistered ? '已报名' : '未报名'}
        </Text>
      </Group>
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
  const part = ParticipationStatusMap.get(participation.status!)!

  return (
    <Accordion.Item value={participation.id!.toString()}>
      <Box sx={{ display: 'flex', alignItems: 'center' }}>
        <Accordion.Control>
          <Group position="apart">
            <Group>
              <Avatar alt="avatar" src={participation.team?.avatar}>
                {!participation.team?.name ? 'T' : participation.team.name.slice(0, 1)}
              </Avatar>
              <Box>
                <Text fw={500}>
                  {!participation.team?.name ? '（无名队伍）' : participation.team.name}
                </Text>
                <Text size="sm" c="dimmed">
                  {!participation.team?.bio ? '（未设置签名）' : participation.team.bio}
                </Text>
              </Box>
            </Group>
            <Group position="apart" w="30%">
              <Box>
                <Text>{participation.organization}</Text>
                <Text size="sm" c="dimmed" fw={700}>
                  {participation.registeredMembers?.length ?? 0}/
                  {participation.team?.members?.length ?? 0} 已报名
                </Text>
              </Box>
              <Box w="6em">
                <Badge color={part.color}>{part.title}</Badge>
              </Box>
            </Group>
          </Group>
        </Accordion.Control>
        <ParticipationStatusControl
          disabled={disabled}
          participateId={participation.id!}
          status={participation.status!}
          setParticipationStatus={setParticipationStatus}
        />
      </Box>
      <Accordion.Panel>
        <Stack>
          {participation.team?.members?.map((user) => (
            <MemberItem
              key={user.userId}
              user={user}
              isRegistered={
                participation.registeredMembers?.some((u) => u === user.userId) ?? false
              }
              isCaptain={participation.team?.captainId === user.userId}
            />
          ))}
        </Stack>
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
  const { classes } = useAccordionStyles()

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
          <Group w="calc(100% - 9rem)" position="left">
            <Select
              placeholder="全部显示"
              clearable
              data={Array.from(ParticipationStatusMap, (v) => ({ value: v[0], label: v[1].title }))}
              value={selectedStatus}
              onChange={(value: ParticipationStatus) => setSelectedStatus(value)}
            />
          </Group>
        </>
      }
    >
      <ScrollArea type="auto" pos="relative" h="calc(100vh - 180px)" offsetScrollbars>
        {!participations || participations.length === 0 ? (
          <Center h="calc(100vh - 200px)">
            <Stack spacing={0}>
              <Title order={2}>Ouch! 还没有队伍报名这个比赛</Title>
              <Text>在路上了……别急！</Text>
            </Stack>
          </Center>
        ) : (
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
        )}
      </ScrollArea>
    </WithGameEditTab>
  )
}

export default GameTeamReview

import { FC, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import {
  Alert,
  Anchor,
  BackgroundImage,
  Badge,
  Button,
  Center,
  Container,
  Group,
  Stack,
  Text,
  Title,
} from '@mantine/core'
import { useScrollIntoView } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiAlertCircle, mdiCheck, mdiFlagOutline, mdiTimerSand } from '@mdi/js'
import { Icon } from '@mdi/react'
import CustomProgress from '@Components/CustomProgress'
import GameJoinModal from '@Components/GameJoinModal'
import MarkdownRender from '@Components/MarkdownRender'
import WithNavBar from '@Components/WithNavbar'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useBannerStyles, useIsMobile } from '@Utils/ThemeOverride'
import { getGameStatus, useGame } from '@Utils/useGame'
import { usePageTitle } from '@Utils/usePageTitle'
import { useTeams, useUser } from '@Utils/useUser'
import api, { GameJoinModel, ParticipationStatus } from '@Api'

const GameAlertMap = new Map([
  [
    ParticipationStatus.Pending,
    {
      color: 'yellow',
      icon: mdiTimerSand,
      label: '你已经以队伍 {TEAM} 成员身份成功报名',
      content: '请耐心等待审核结果',
    },
  ],
  [ParticipationStatus.Accepted, null],
  [
    ParticipationStatus.Rejected,
    {
      color: 'red',
      icon: mdiAlertCircle,
      label: '您的参赛申请未通过',
      content: '请确保具备参赛资格和满足参赛要求后重新报名',
    },
  ],
  [
    ParticipationStatus.Suspended,
    {
      color: 'red',
      icon: mdiAlertCircle,
      label: '您的队伍 {TEAM} 已被禁赛',
      content: '如有异议，请联系管理员进行申诉',
    },
  ],
  [ParticipationStatus.Unsubmitted, null],
])

const GameActionMap = new Map([
  [ParticipationStatus.Pending, '等待审核'],
  [ParticipationStatus.Accepted, '通过审核'],
  [ParticipationStatus.Rejected, '重新报名'],
  [ParticipationStatus.Suspended, '通过审核'],
  [ParticipationStatus.Unsubmitted, '报名参赛'],
])

const GetAlert = (status: ParticipationStatus, team: string) => {
  const data = GameAlertMap.get(status)
  if (data) {
    return (
      <Alert
        color={data.color}
        icon={<Icon path={data.icon} />}
        title={data.label.replace('{TEAM}', team)}
      >
        {data.content}
      </Alert>
    )
  }
  return null
}

const GameDetail: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const navigate = useNavigate()

  const { game, error, mutate, status } = useGame(numId)

  const { classes, theme } = useBannerStyles()

  const { startTime, endTime, finished, started, progress } = getGameStatus(game)

  const { user } = useUser()
  const { teams } = useTeams()

  const modals = useModals()
  const isMobile = useIsMobile()

  usePageTitle(game?.title)

  useEffect(() => {
    if (error) {
      showErrorNotification(error)
      navigate('/games')
    }
  }, [error])

  const { scrollIntoView, targetRef } = useScrollIntoView<HTMLDivElement>()

  const [joinModalOpen, setJoinModalOpen] = useState(false)

  useEffect(() => scrollIntoView({ alignment: 'center' }), [])

  const onSubmitJoin = async (info: GameJoinModel) => {
    try {
      if (!numId) return

      await api.game.gameJoinGame(numId, info)
      showNotification({
        color: 'teal',
        message: '报名成功',
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutate()
    } catch (err) {
      return showErrorNotification(err)
    }
  }

  const onSubmitLeave = async () => {
    try {
      if (!numId) return
      await api.game.gameLeaveGame(numId)

      showNotification({
        color: 'teal',
        message: '退出成功',
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutate()
    } catch (err) {
      return showErrorNotification(err)
    }
  }

  const canSubmit =
    (status === ParticipationStatus.Unsubmitted || status === ParticipationStatus.Rejected) &&
    !finished &&
    user &&
    teams &&
    teams.length > 0

  const teamRequire =
    user && status === ParticipationStatus.Unsubmitted && !finished && teams && teams.length === 0

  const onJoin = () =>
    modals.openConfirmModal({
      title: '确认报名',
      children: (
        <Stack spacing="xs">
          <Text size="sm">你确定要报名此比赛吗？</Text>
          <Text size="sm">
            报名参赛并审核通过后，参赛队伍将被锁定，不能再进行人员变动。
            <Text span fw={700}>
              即邀请、踢出队员。
            </Text>
            队伍将在比赛结束后或驳回请求时解锁。
          </Text>
          <Text size="sm">
            比赛队伍人数要求以选择队伍的成员数为准，
            <Text span fw={700}>
              不论队员是否以此队伍身份参加比赛。
            </Text>
          </Text>
        </Stack>
      ),
      onConfirm: () => setJoinModalOpen(true),
      labels: { confirm: '确认报名', cancel: '取消' },
      confirmProps: { color: 'brand' },
    })

  const onLeave = () =>
    modals.openConfirmModal({
      title: '确认退出比赛',
      children: (
        <Stack spacing="xs">
          <Text size="sm">你确定要退出此比赛吗？</Text>
          <Text size="sm">退出后如果队伍报名人数为空，队伍参与信息将被删除。</Text>
        </Stack>
      ),
      onConfirm: onSubmitLeave,
      labels: { confirm: '确认退出', cancel: '取消' },
      confirmProps: { color: 'brand' },
    })

  const ControlButtons = (
    <>
      <Button disabled={!canSubmit} onClick={onJoin}>
        {finished ? '比赛结束' : !user ? '请先登录' : GameActionMap.get(status)}
      </Button>
      {started && <Button onClick={() => navigate(`/games/${numId}/scoreboard`)}>查看榜单</Button>}
      {(status === ParticipationStatus.Pending || status === ParticipationStatus.Rejected) && (
        <Button color="red" variant="outline" onClick={onLeave}>
          退出比赛
        </Button>
      )}
      {status === ParticipationStatus.Accepted &&
        started &&
        !isMobile &&
        (!finished || game?.practiceMode) && (
          <Button onClick={() => navigate(`/games/${numId}/challenges`)}>进入比赛</Button>
        )}
    </>
  )

  return (
    <WithNavBar width="100%" isLoading={!game} minWidth={0} withFooter>
      <div ref={targetRef} className={classes.root}>
        <Group
          noWrap
          position="apart"
          w="100%"
          p={`0 ${theme.spacing.md}`}
          className={classes.container}
        >
          <Stack spacing={6} className={classes.flexGrowAtSm}>
            <Group>
              <Badge variant="outline">
                {game?.limit === 0 ? '多' : game?.limit === 1 ? '个' : game?.limit}人赛
              </Badge>
              {game?.hidden && <Badge variant="outline">比赛已隐藏</Badge>}
            </Group>
            <Stack spacing={2}>
              <Title className={classes.title}>{game?.title}</Title>
              <Text size="sm" c="dimmed">
                <Text span fw={700}>
                  {`${game?.teamCount ?? 0} `}
                </Text>
                支队伍已报名
              </Text>
            </Stack>
            <Group position="apart">
              <Stack spacing={0}>
                <Text size="sm" className={classes.date}>
                  开始时间
                </Text>
                <Text size="sm" fw={700} className={classes.date}>
                  {startTime.format('HH:mm:ss, MMMM DD, YYYY')}
                </Text>
              </Stack>
              <Stack spacing={0}>
                <Text size="sm" className={classes.date}>
                  结束时间
                </Text>
                <Text size="sm" fw={700} className={classes.date}>
                  {endTime.format('HH:mm:ss, MMMM DD, YYYY')}
                </Text>
              </Stack>
            </Group>
            <CustomProgress percentage={progress} />
            <Group>{ControlButtons}</Group>
          </Stack>
          <BackgroundImage className={classes.banner} src={game?.poster ?? ''} radius="sm">
            <Center h="100%">
              {!game?.poster && (
                <Icon path={mdiFlagOutline} size={4} color={theme.colors.gray[5]} />
              )}
            </Center>
          </BackgroundImage>
        </Group>
      </div>
      <Container className={classes.content}>
        <Stack spacing="xs" pb={100}>
          {GetAlert(status, game?.teamName ?? '')}
          {teamRequire && (
            <Alert color="yellow" icon={<Icon path={mdiAlertCircle} />} title="当前无法报名">
              你没有加入任何队伍，请在
              <Anchor component={Link} to="/teams">
                队伍管理
              </Anchor>
              页面创建、加入队伍。
            </Alert>
          )}
          {status === ParticipationStatus.Accepted && !started && (
            <Alert color="teal" icon={<Icon path={mdiCheck} />} title="比赛尚未开始">
              你已经以队伍 "{game?.teamName}" 成员身份成功报名并通过审核，请耐心等待比赛开始。
              {isMobile && '请使用电脑端参与比赛及查看比赛详情。'}
            </Alert>
          )}
          <MarkdownRender source={game?.content ?? ''} />
        </Stack>
        <GameJoinModal
          title="补全报名信息"
          opened={joinModalOpen}
          withCloseButton={false}
          onClose={() => setJoinModalOpen(false)}
          onSubmitJoin={onSubmitJoin}
        />
      </Container>
    </WithNavBar>
  )
}

export default GameDetail

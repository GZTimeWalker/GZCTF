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
import { FC, useEffect, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { Link, useNavigate, useParams } from 'react-router-dom'
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

const GetAlert = (status: ParticipationStatus, team: string) => {
  const { t } = useTranslation()

  const GameAlertMap = new Map([
    [
      ParticipationStatus.Pending,
      {
        color: 'yellow',
        icon: mdiTimerSand,
        title: t('game.participation.alert.pending.title', { team }),
        content: t('game.participation.alert.pending.content'),
      },
    ],
    [ParticipationStatus.Accepted, null],
    [
      ParticipationStatus.Rejected,
      {
        color: 'red',
        icon: mdiAlertCircle,
        title: t('game.participation.alert.rejected.title'),
        content: t('game.participation.alert.rejected.content'),
      },
    ],
    [
      ParticipationStatus.Suspended,
      {
        color: 'red',
        icon: mdiAlertCircle,
        title: t('game.participation.alert.suspended.title', { team }),
        content: t('game.participation.alert.suspended.content'),
      },
    ],
    [ParticipationStatus.Unsubmitted, null],
  ])

  const data = GameAlertMap.get(status)
  if (data) {
    return (
      <Alert color={data.color} icon={<Icon path={data.icon} />} title={data.title}>
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

  const { t } = useTranslation()

  usePageTitle(game?.title)

  useEffect(() => {
    if (error) {
      showErrorNotification(error, t)
      navigate('/games')
    }
  }, [error])

  const { scrollIntoView, targetRef } = useScrollIntoView<HTMLDivElement>()

  const [joinModalOpen, setJoinModalOpen] = useState(false)

  useEffect(() => scrollIntoView({ alignment: 'center' }), [])

  const GameActionMap = new Map([
    [ParticipationStatus.Pending, t('game.participation.actions.pending')],
    [ParticipationStatus.Accepted, t('game.participation.actions.accepted')],
    [ParticipationStatus.Rejected, t('game.participation.actions.rejected')],
    [ParticipationStatus.Suspended, t('game.participation.actions.suspended')],
    [ParticipationStatus.Unsubmitted, t('game.participation.actions.unsubmitted')],
  ])

  const onSubmitJoin = async (info: GameJoinModel) => {
    try {
      if (!numId) return

      await api.game.gameJoinGame(numId, info)
      showNotification({
        color: 'teal',
        message: t('game.notification.joined'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutate()
    } catch (err) {
      return showErrorNotification(err, t)
    }
  }

  const onSubmitLeave = async () => {
    try {
      if (!numId) return
      await api.game.gameLeaveGame(numId)

      showNotification({
        color: 'teal',
        message: t('game.notification.left'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutate()
    } catch (err) {
      return showErrorNotification(err, t)
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
      title: t('game.content.join.confirm'),
      children: (
        <Stack spacing="xs">
          <Text size="sm">{t('game.content.join.content.0')}</Text>
          <Text size="sm">
            <Trans i18nKey="game.content.join.content.1" />
          </Text>
          <Text size="sm">
            <Trans i18nKey="game.content.join.content.2" />
          </Text>
        </Stack>
      ),
      onConfirm: () => setJoinModalOpen(true),
      confirmProps: { color: 'brand' },
    })

  const onLeave = () =>
    modals.openConfirmModal({
      title: t('game.content.leave.confirm'),
      children: (
        <Stack spacing="xs">
          <Text size="sm">{t('game.content.leave.content.0')}</Text>
          <Text size="sm">{t('game.content.leave.content.1')}</Text>
        </Stack>
      ),
      onConfirm: onSubmitLeave,
      confirmProps: { color: 'brand' },
    })

  const ControlButtons = (
    <>
      <Button disabled={!canSubmit} onClick={onJoin}>
        {finished
          ? t('game.button.finished')
          : !user
            ? t('game.button.login_required')
            : GameActionMap.get(status)}
      </Button>
      {started && (
        <Button onClick={() => navigate(`/games/${numId}/scoreboard`)}>
          {t('game.button.scoreboard')}
        </Button>
      )}
      {(status === ParticipationStatus.Pending || status === ParticipationStatus.Rejected) && (
        <Button color="red" variant="outline" onClick={onLeave}>
          {t('game.button.leave')}
        </Button>
      )}
      {status === ParticipationStatus.Accepted &&
        started &&
        !isMobile &&
        (!finished || game?.practiceMode) && (
          <Button onClick={() => navigate(`/games/${numId}/challenges`)}>
            {t('game.button.challenges')}
          </Button>
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
                {!game || game.limit === 0
                  ? t('game.tag.mutiplayer')
                  : game.limit === 1
                    ? t('game.tag.individual')
                    : t('game.tag.limited', { count: game.limit })}
              </Badge>
              {game?.hidden && <Badge variant="outline">{t('game.tag.hidden')}</Badge>}
            </Group>
            <Stack spacing={2}>
              <Title className={classes.title}>{game?.title}</Title>
              <Text size="sm" c="dimmed">
                <Trans
                  i18nKey="game.content.joined_status"
                  values={{ count: game?.teamCount ?? 0 }}
                />
              </Text>
            </Stack>
            <Group position="apart">
              <Stack spacing={0}>
                <Text size="sm" className={classes.date}>
                  {t('game.content.start_time')}
                </Text>
                <Text size="sm" fw={700} className={classes.date}>
                  {startTime.format('HH:mm:ss, MMMM DD, YYYY')}
                </Text>
              </Stack>
              <Stack spacing={0}>
                <Text size="sm" className={classes.date}>
                  {t('game.content.end_time')}
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
            <Alert
              color="yellow"
              icon={<Icon path={mdiAlertCircle} />}
              title={t('game.participation.alert.team_required.title')}
            >
              <Trans i18nKey="game.participation.alert.team_required.content">
                _
                <Anchor component={Link} to="/teams">
                  _
                </Anchor>
                _
              </Trans>
            </Alert>
          )}
          {status === ParticipationStatus.Accepted && !started && (
            <Alert
              color="teal"
              icon={<Icon path={mdiCheck} />}
              title={t('game.participation.alert.not_started.title')}
            >
              {t('game.participation.alert.not_started.content', {
                team: game?.teamName ?? '',
              })}
              {isMobile && t('game.participation.alert.not_started.mobile')}
            </Alert>
          )}
          <MarkdownRender source={game?.content ?? ''} />
        </Stack>
        <GameJoinModal
          title={t('game.content.join.title')}
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

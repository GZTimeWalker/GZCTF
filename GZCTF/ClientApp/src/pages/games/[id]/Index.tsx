import dayjs from 'dayjs'
import { marked } from 'marked'
import { FC, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Image,
  Button,
  Container,
  createStyles,
  Group,
  Stack,
  Text,
  Title,
  TypographyStylesProvider,
  Center,
  Progress,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiFlagOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { ParticipationStatus } from '../../../Api'
import WithNavBar from '../../../components/WithNavbar'
import { showErrorNotification } from '../../../utils/ApiErrorHandler'

const useStyles = createStyles((theme) => ({
  root: {
    position: 'relative',
    height: '40vh',
    display: 'flex',
    background: ` rgba(0,0,0,0.2)`,
    justifyContent: 'center',
    backgroundSize: 'cover',
    backgroundPosition: 'center center',
    padding: `${theme.spacing.xl}px ${theme.spacing.xl * 4}px`,
  },
  container: {
    position: 'relative',
    maxWidth: '960px',
    width: '100%',
    margin: '0 auto',
    zIndex: 1,
  },
  description: {
    color: theme.white,
    maxWidth: 600,
  },
  title: {
    color: theme.white,
    fontSize: 50,
    fontWeight: 900,
    lineHeight: 1.1,
  },
  content: {
    minHeight: '100vh',
  },
}))

const GameDetail: FC = () => {
  const { id } = useParams()
  const numId = parseInt(id ?? '-1')
  const navigate = useNavigate()

  const { data: game, error } = api.game.useGameGames(parseInt(id!), {
    refreshInterval: 0,
  })

  const { classes, theme } = useStyles()

  const startTime = dayjs(game?.start) ?? dayjs()
  const endTime = dayjs(game?.end) ?? dayjs()
  const duriation = endTime.diff(startTime, 'minute')
  const current = dayjs().diff(startTime, 'minute')

  const finished = current > duriation
  const progress = finished ? 100 : current / duriation

  const { data: user } = api.account.useAccountProfile({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  useEffect(() => {
    if (error) {
      showErrorNotification(error)
      navigate('/games')
    }
  }, [error])

  const status = game?.status ?? ParticipationStatus.Unsubmitted
  const modals = useModals()

  const onSubmit = () => {
    api.game.gameJoinGame(numId ?? 0).then(() => {
      showNotification({
        color: 'teal',
        message: '成功报名，请等待审核',
        icon: <Icon path={mdiCheck} size={1} />,
        disallowClose: true,
      })
    })
  }

  const canSubmit =
    status === ParticipationStatus.Unsubmitted &&
    !finished &&
    user &&
    user.ownTeamId &&
    user.activeTeamId === user.ownTeamId

  const ControlButton = (
    <Button
      disabled={!canSubmit}
      onClick={() => {
        modals.openConfirmModal({
          title: '确认报名',
          children: (
            <Stack spacing="xs">
              <Text size="sm">你确定要报名此比赛吗？</Text>
              <Text size="sm">
                报名参赛后当前队伍将被锁定，不能再进行人员变动。即邀请、踢出队员。队伍将在比赛结束后或驳回请求时解锁。
              </Text>
            </Stack>
          ),
          onConfirm: onSubmit,
          centered: true,
          labels: { confirm: '确认报名', cancel: '取消' },
          confirmProps: { color: 'brand' },
        })
      }}
    >
      {status === ParticipationStatus.Pending ? '等待审核' : '报名参赛'}
    </Button>
  )

  return (
    <WithNavBar width="100%" padding={0} isLoading={!game}>
      <div className={classes.root}>
        <Group noWrap position="apart" style={{ width: '100%' }} className={classes.container}>
          <Stack spacing="xs">
            <Title className={classes.title}>{game?.title}</Title>
            <Group>
              <Stack spacing={0}>
                <Text size="sm" color="white">
                  开始时间
                </Text>
                <Text size="sm" weight={700} color="white">
                  {startTime.format('HH:mm:ss, MMMM DD, YYYY')}
                </Text>
              </Stack>
              <Stack spacing={0}>
                <Text size="sm" color="white">
                  结束时间
                </Text>
                <Text size="sm" weight={700} color="white">
                  {endTime.format('HH:mm:ss, MMMM DD, YYYY')}
                </Text>
              </Stack>
            </Group>
            <Progress size="xs" radius="xs" value={progress} />
            <Group>{ControlButton}</Group>
          </Stack>
          <Center style={{ width: '40%' }}>
            {game && game?.poster ? (
              <Image src={game.poster} alt="poster" />
            ) : (
              <Icon path={mdiFlagOutline} size={4} color={theme.colors.gray[5]} />
            )}
          </Center>
        </Group>
      </div>
      <Container className={classes.content}>
        <Group noWrap align="flex-start">
          <TypographyStylesProvider p="2rem 0">
            <div dangerouslySetInnerHTML={{ __html: marked(game?.content ?? '') }} />
          </TypographyStylesProvider>
        </Group>
      </Container>
    </WithNavBar>
  )
}

export default GameDetail

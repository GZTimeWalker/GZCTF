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
} from '@mantine/core'
import { mdiFlagOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import api from '../../../Api'
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
  const navigate = useNavigate()

  const { data: game, error } = api.game.useGameGames(parseInt(id!), {
    refreshInterval: 0,
  })

  const { classes, theme } = useStyles()

  useEffect(() => {
    if (error) {
      showErrorNotification(error)
      navigate('/games')
    }
  }, [error])

  return (
    <WithNavBar width="100%" padding={0} isLoading={!game}>
      <div className={classes.root}>
        <Group noWrap position="apart" style={{ width: '100%' }} className={classes.container}>
          <Stack>
            <Title className={classes.title}>{game?.title}</Title>
            <Group>
              <Stack spacing={0}>
                <Text size="sm" color="white">
                  开始时间
                </Text>
                <Text size="sm" weight={700} color="white">
                  {dayjs(game?.start).format('HH:mm:ss, MMMM DD, YYYY')}
                </Text>
              </Stack>
              <Stack spacing={0}>
                <Text size="sm" color="white">
                  结束时间
                </Text>
                <Text size="sm" weight={700} color="white">
                  {dayjs(game?.end).format('HH:mm:ss, MMMM DD, YYYY')}
                </Text>
              </Stack>
            </Group>
            <Group>
              <Button>报名参赛</Button>
            </Group>
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

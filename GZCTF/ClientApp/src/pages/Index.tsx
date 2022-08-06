import { FC } from 'react'
import { createStyles, Group, keyframes, Stack, Title } from '@mantine/core'
import { mdiFlagCheckered } from '@mdi/js'
import { Icon } from '@mdi/react'
import LogoHeader from '@Components/LogoHeader'
import NoticeCard from '@Components/NoticeCard'
import RecentGame from '@Components/RecentGame'
import WithNavBar from '@Components/WithNavbar'
import api from '@Api/Api'

const useStyles = createStyles((theme) => ({
  notices: {
    width: '78%',

    [`@media (max-width: 1080px)`]: {
      width: '100%',
    },
  },
  wrapper: {
    boxSizing: 'border-box',
    paddingLeft: theme.spacing.md,
    position: 'sticky',
    top: theme.spacing.xs,
    right: 0,
    paddingTop: 10,
    flex: `0 0`,

    [`@media (max-width: 1080px)`]: {
      display: 'none',
    },
  },
  inner: {
    paddingTop: 0,
    paddingBottom: theme.spacing.xl,
    paddingLeft: theme.spacing.md,
    width: '15vw',
    minWidth: '230px',
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'space-between',
  },
  blink: {
    animation: `${keyframes`0%, 100% {opacity:0;} 50% {opacity:1;}`} 1s infinite steps(1,start)`,
  },
  subtitle: {
    fontFamily: theme.fontFamilyMonospace,
    color: theme.colorScheme === 'dark' ? theme.colors.gray[4] : theme.colors.dark[4],

    [`@media (max-width: 900px)`]: {
      display: 'none',
    },
  },
}))

const Home: FC = () => {
  const { data: notices } = api.info.useInfoGetNotices({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const { data: allGames } = api.game.useGameGamesAll({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  allGames?.sort((a, b) => new Date(a.end!).getTime() - new Date(b.end!).getTime())

  const now = new Date()
  const recentGames = [
    ...(allGames?.filter((g) => now < new Date(g.end ?? '')) ?? []),
    ...(allGames?.filter((g) => now >= new Date(g.end ?? '')).reverse() ?? []),
  ].slice(0, 3)

  const { classes, theme } = useStyles()

  return (
    <WithNavBar>
      <Stack align="center">
        <Group
          position="apart"
          align="flex-end"
          sx={{
            width: '100%',
            [theme.fn.smallerThan('xs')]: {
              display: 'none',
            },
          }}
        >
          <LogoHeader />
          <Title className={classes.subtitle} order={3}>
            &gt; Hack for fun not for profit<span className={classes.blink}>_</span>
          </Title>
        </Group>
        <Group noWrap spacing={4} position="apart" align="flex-start" style={{ width: '100%' }}>
          <Stack className={classes.notices}>
            {notices?.map((notice) => (
              <NoticeCard key={notice.id} {...notice} />
            ))}
          </Stack>
          <nav className={classes.wrapper}>
            <div className={classes.inner}>
              <Stack>
                <Group>
                  <Icon path={mdiFlagCheckered} size={1.5} color={theme.colors.brand[4]} />
                  <Title order={3}>近期活动</Title>
                </Group>
                {recentGames?.map((game) => (
                  <RecentGame game={game} />
                ))}
              </Stack>
            </div>
          </nav>
        </Group>
      </Stack>
    </WithNavBar>
  )
}

export default Home

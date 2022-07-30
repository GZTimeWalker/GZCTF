import { FC } from 'react'
import { createStyles, Grid, Group, keyframes, Stack, Title } from '@mantine/core'
import { mdiFlagCheckered } from '@mdi/js'
import { Icon } from '@mdi/react'
import api from '../Api'
import LogoHeader from '../components/LogoHeader'
import NoticeCard from '../components/NoticeCard'
import RecentGame from '../components/RecentGame'
import WithNavBar from '../components/WithNavbar'

const useStyles = createStyles((theme) => ({
  wrapper: {
    boxSizing: 'border-box',
    paddingLeft: theme.spacing.md,
    position: 'sticky',
    top: theme.spacing.xs,
    right: 0,
    paddingTop: 10,
    flex: `0 0 240px`,

    [`@media (max-width: 1080px)`]: {
      display: 'none',
    },
  },
  inner: {
    paddingTop: 0,
    paddingBottom: theme.spacing.xl,
    paddingLeft: theme.spacing.md,
    display: 'flex',
    flexDirection: 'column',
    justifyContent: 'space-between',
  },
  blink: {
    animation: `${keyframes`0%, 100% {opacity:0;} 50% {opacity:1;}`} 1s infinite steps(1,start)`,
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
    ...(allGames?.filter((g) => now < new Date(g.end ?? '')).slice(0, 3) ?? []),
    ...(allGames?.filter((g) => now >= new Date(g.end ?? '')).reverse() ?? []),
  ].slice(0, 3)

  const { classes, theme } = useStyles()

  return (
    <WithNavBar>
      <Stack align="center">
        <Group position="apart" align="flex-end" style={{ width: '100%' }}>
          <LogoHeader />
          <Title
            style={{
              fontFamily: theme.fontFamilyMonospace,
              color: theme.colorScheme === 'dark' ? theme.colors.gray[4] : theme.colors.dark[4],
            }}
            order={3}
          >
            &gt; Hack for fun not for profit<span className={classes.blink}>_</span>
          </Title>
        </Group>
        <Grid style={{ width: '100%' }}>
          <Grid.Col lg={recentGames.length > 0 ? 9 : 12} md={6}>
            <Stack>
              {notices?.map((notice) => (
                <NoticeCard key={notice.id} {...notice} />
              ))}
            </Stack>
          </Grid.Col>
          {recentGames.length > 0 &&
            <Grid.Col lg={3} md={6}>
              <nav className={classes.wrapper}>
                <div className={classes.inner}>
                  <Stack>
                    <Group>
                      <Icon path={mdiFlagCheckered} size={1.5} color={theme.colors.brand[4]} />
                      <Title order={3}>近期活动</Title>
                    </Group>
                    {recentGames?.map((game) => (
                      <RecentGame key={game.id} game={game} />
                    ))}
                  </Stack>
                </div>
              </nav>
            </Grid.Col>
          }
        </Grid>
      </Stack>
    </WithNavBar>
  )
}

export default Home

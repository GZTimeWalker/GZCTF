import { FC } from 'react'
import { createStyles, Group, Stack, Title } from '@mantine/core'
import { mdiFlagCheckered } from '@mdi/js'
import { Icon } from '@mdi/react'
import PostCard from '@Components/PostCard'
import RecentGame from '@Components/RecentGame'
import StickyHeader from '@Components/StickyHeader'
import WithNavBar from '@Components/WithNavbar'
import { usePageTitle } from '@Utils/PageTitle'
import api from '@Api'

const useStyles = createStyles((theme) => ({
  posts: {
    width: '78%',

    [`@media (max-width: 1080px)`]: {
      width: '100%',
    },
  },
  wrapper: {
    boxSizing: 'border-box',
    paddingLeft: theme.spacing.md,
    position: 'sticky',
    top: theme.spacing.xs + 74,
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
}))

const Home: FC = () => {
  const { data: posts } = api.info.useInfoGetPosts({
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
  usePageTitle()

  return (
    <WithNavBar>
      <StickyHeader />
      <Stack align="center">
        <Group noWrap spacing={4} position="apart" align="flex-start" style={{ width: '100%' }}>
          <Stack className={classes.posts}>
            {posts?.map((notice, idx) => (
              <PostCard key={idx} {...notice} />
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
                  <RecentGame key={game.id} game={game} />
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

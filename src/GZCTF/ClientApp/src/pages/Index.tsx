import { Group, Stack, Title, useMantineTheme } from '@mantine/core'
import { useViewportSize } from '@mantine/hooks'
import { mdiFlagCheckered } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { PostCard } from '@Components/PostCard'
import { RecentGame } from '@Components/RecentGame'
import { WithNavBar } from '@Components/WithNavbar'
import { MobilePostCard } from '@Components/mobile/PostCard'
import { RecentGameCarousel } from '@Components/mobile/RecentGameCarousel'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useIsMobile } from '@Utils/ThemeOverride'
import { useRecentGames } from '@Hooks/useGame'
import { usePageTitle } from '@Hooks/usePageTitle'
import api, { PostInfoModel } from '@Api'
import classes from '@Styles/Index.module.css'

const Home: FC = () => {
  const { t } = useTranslation()

  const { data: posts, mutate } = api.info.useInfoGetLatestPosts({
    refreshInterval: 5 * 60 * 1000,
  })

  const { recentGames } = useRecentGames()

  const onTogglePinned = async (post: PostInfoModel, setDisabled: (value: boolean) => void) => {
    setDisabled(true)

    try {
      const res = await api.edit.editUpdatePost(post.id, {
        title: post.title,
        isPinned: !post.isPinned,
      })
      if (post.isPinned) {
        mutate([
          ...(posts?.filter((p) => p.id !== post.id && p.isPinned) ?? []),
          { ...res.data },
          ...(posts?.filter((p) => p.id !== post.id && !p.isPinned) ?? []),
        ])
      } else {
        mutate([
          { ...res.data },
          ...(posts?.filter((p) => p.id !== post.id && p.isPinned) ?? []),
          ...(posts?.filter((p) => p.id !== post.id && !p.isPinned) ?? []),
        ])
      }
      api.info.mutateInfoGetPosts()
    } catch (e) {
      showErrorNotification(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const theme = useMantineTheme()
  const isMobile = useIsMobile(900)
  const { height } = useViewportSize()

  const showGames = isMobile ? recentGames : recentGames?.slice(0, Math.ceil((height - 300) / 240))

  usePageTitle()

  return (
    <WithNavBar minWidth={0} withFooter withHeader stickyHeader>
      <Stack justify="flex-start">
        {isMobile && showGames && showGames.length > 0 && <RecentGameCarousel games={showGames} />}
        <Stack align="center">
          <Group wrap="nowrap" gap={4} justify="space-between" align="flex-start" w="100%">
            <Stack className={classes.posts}>
              {isMobile
                ? posts?.map((post) => <MobilePostCard key={post.id} post={post} onTogglePinned={onTogglePinned} />)
                : posts?.map((post) => <PostCard key={post.id} post={post} onTogglePinned={onTogglePinned} />)}
            </Stack>
            {!isMobile && (
              <nav className={classes.wrapper}>
                <div className={classes.inner}>
                  <Stack>
                    <Group wrap="nowrap">
                      <Icon path={mdiFlagCheckered} size={1.5} color={theme.colors[theme.primaryColor][4]} />
                      <Title order={3}>{t('common.content.home.recent_games')}</Title>
                    </Group>
                    {showGames?.map((game) => <RecentGame key={game.id} game={game} />)}
                  </Stack>
                </div>
              </nav>
            )}
          </Group>
        </Stack>
      </Stack>
    </WithNavBar>
  )
}

export default Home

import { Stack } from '@mantine/core'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { PostCard } from '@Components/PostCard'
import { WithNavBar } from '@Components/WithNavbar'
import { MobilePostCard } from '@Components/mobile/PostCard'
import { showErrorMsg } from '@Utils/Shared'
import { useIsMobile } from '@Utils/ThemeOverride'
import { usePageTitle } from '@Hooks/usePageTitle'
import api, { PostInfoModel } from '@Api'
import classes from '@Styles/Index.module.css'

const Home: FC = () => {
  const { t } = useTranslation()

  const { data: posts, mutate } = api.info.useInfoGetLatestPosts({
    refreshInterval: 5 * 60 * 1000,
  })

  const onTogglePinned = async (post: PostInfoModel, setDisabled: (value: boolean) => void) => {
    setDisabled(true)

    try {
      const res = await api.editPosts.editPostsUpdate(post.id, {
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
      showErrorMsg(e, t)
    } finally {
      setDisabled(false)
    }
  }

  const isMobile = useIsMobile(900)

  usePageTitle()

  return (
    <WithNavBar minWidth={0} withFooter withHeader stickyHeader>
      <Stack justify="flex-start">
        <Stack align="center">
          <Stack className={classes.posts}>
            {isMobile
              ? posts?.map((post) => <MobilePostCard key={post.id} post={post} onTogglePinned={onTogglePinned} />)
              : posts?.map((post) => <PostCard key={post.id} post={post} onTogglePinned={onTogglePinned} />)}
          </Stack>
        </Stack>
      </Stack>
    </WithNavBar>
  )
}

export default Home

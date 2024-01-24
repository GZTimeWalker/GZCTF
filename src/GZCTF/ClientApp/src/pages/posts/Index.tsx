import { Button, Pagination, Stack } from '@mantine/core'
import { mdiPlus } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import PostCard from '@Components/PostCard'
import StickyHeader from '@Components/StickyHeader'
import WithNavBar from '@Components/WithNavbar'
import { RequireRole } from '@Components/WithRole'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useFixedButtonStyles } from '@Utils/ThemeOverride'
import { OnceSWRConfig } from '@Utils/useConfig'
import { usePageTitle } from '@Utils/usePageTitle'
import { useUserRole } from '@Utils/useUser'
import api, { PostInfoModel, Role } from '@Api'

const ITEMS_PER_PAGE = 10

const Posts: FC = () => {
  const { data: posts, mutate } = api.info.useInfoGetPosts(OnceSWRConfig)

  const { classes: btnClasses } = useFixedButtonStyles({
    right: 'calc(0.1 * (100vw - 70px - 2rem) + 1rem)',
    bottom: '2rem',
  })
  const [activePage, setPage] = useState(1)
  const navigate = useNavigate()
  const { role } = useUserRole()

  const { t } = useTranslation()

  usePageTitle(t('post.title.index'))

  const onTogglePinned = (post: PostInfoModel, setDisabled: (value: boolean) => void) => {
    setDisabled(true)
    api.edit
      .editUpdatePost(post.id, { title: post.title, isPinned: !post.isPinned })
      .then((res) => {
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
        api.info.mutateInfoGetLatestPosts()
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <WithNavBar isLoading={!posts} minWidth={0}>
      <Stack justify="space-between" mb="3rem">
        <StickyHeader />
        <Stack>
          {posts
            ?.slice((activePage - 1) * ITEMS_PER_PAGE, activePage * ITEMS_PER_PAGE)
            .map((post) => <PostCard key={post.id} post={post} onTogglePinned={onTogglePinned} />)}
        </Stack>
        {(posts?.length ?? 0) > ITEMS_PER_PAGE && (
          <Pagination
            position="center"
            my={20}
            value={activePage}
            onChange={setPage}
            total={Math.ceil((posts?.length ?? 0) / ITEMS_PER_PAGE)}
          />
        )}
      </Stack>
      {RequireRole(Role.Admin, role) && (
        <Button
          className={btnClasses.fixedButton}
          variant="filled"
          radius="xl"
          size="md"
          leftIcon={<Icon path={mdiPlus} size={1} />}
          onClick={() => navigate('/posts/new/edit')}
        >
          {t('post.button.new')}
        </Button>
      )}
    </WithNavBar>
  )
}

export default Posts

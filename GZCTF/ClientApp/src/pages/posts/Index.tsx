import { FC } from 'react'
import { Stack } from '@mantine/core'
import StickyHeader from '@Components/StickyHeader'
import WithNavBar from '@Components/WithNavbar'
import { usePageTitle } from '@Utils/usePageTitle'
import api from '@Api'
import PostCard from '@Components/PostCard'

const Posts: FC = () => {
  const { data: posts } = api.info.useInfoGetPosts({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  usePageTitle('文章')

  return (
    <WithNavBar>
      <StickyHeader />
      <Stack>
        {posts?.map((post) => (
          <PostCard key={post.id} {...post} />
        ))}
      </Stack>
    </WithNavBar>
  )
}

export default Posts

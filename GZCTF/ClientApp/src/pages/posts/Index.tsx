import { FC } from 'react'
import { Stack } from '@mantine/core'
import PostCard from '@Components/PostCard'
import StickyHeader from '@Components/StickyHeader'
import WithNavBar from '@Components/WithNavbar'
import { usePageTitle } from '@Utils/usePageTitle'
import api from '@Api'

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

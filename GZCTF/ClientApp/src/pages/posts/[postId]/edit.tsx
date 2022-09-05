import { FC, useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router'
import { Group, Stack, TextInput, Text } from '@mantine/core'
import StickyHeader from '@Components/StickyHeader'
import WithNavBar from '@Components/WithNavbar'
import api, { PostEditModel } from '@Api'

const PostEdit: FC = () => {
  const { postId } = useParams()
  const navigate = useNavigate()

  useEffect(() => {
    if (postId?.length != 8) {
      navigate('/404')
      return
    }
  }, [postId])

  const { data: curPost } = api.info.useInfoGetPost(
    postId ?? '',
    {
      refreshInterval: 0,
      revalidateIfStale: false,
      revalidateOnFocus: false,
    },
    postId?.length === 8
  )

  const [post, setPost] = useState<PostEditModel>({
    title: curPost?.title ?? '',
    content: curPost?.content ?? '',
    summary: curPost?.summary ?? '',
    isPinned: curPost?.isPinned ?? false,
  })

  return (
    <WithNavBar>
      <StickyHeader />
      <Stack>
        <Group position="apart" style={{ width: '100%' }}>
          <Text weight={700} size={40}>
            #
          </Text>
          <TextInput
            value={post.title}
            onChange={(e) => setPost({ ...post, title: e.currentTarget.value })}
            styles={{
              root: {
                width: 'calc(100% - 3rem)',
              },
              input: {
                height: 60,
                fontSize: 32,
                fontWeight: 500,
              },
            }}
          />
        </Group>
      </Stack>
    </WithNavBar>
  )
}

export default PostEdit

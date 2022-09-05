import { FC, useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router'
import { Group, Stack, TextInput, Text, Title } from '@mantine/core'
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

  useEffect(() => {
    if (curPost) {
      setPost({
        title: curPost.title,
        content: curPost.content,
        summary: curPost.summary,
        isPinned: curPost.isPinned,
      })
    }
  }, [curPost])

  return (
    <WithNavBar>
      <StickyHeader />
      <Stack>
        <Title align='center' order={1}>编辑文章</Title>
        <TextInput
          label="文章标题"
          value={post.title}
          onChange={(e) => setPost({ ...post, title: e.currentTarget.value })}
        />
      </Stack>
    </WithNavBar>
  )
}

export default PostEdit

import dayjs from 'dayjs'
import LocalizedFormat from 'dayjs/plugin/localizedFormat'
import { FC, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Avatar, Container, Divider, Stack, Title, Text, Group } from '@mantine/core'
import { useScrollIntoView } from '@mantine/hooks'
import MarkdownRender from '@Components/MarkdownRender'
import WithNavBar from '@Components/WithNavbar'
import { useBannerStyles } from '@Utils/ThemeOverride'
import { usePageTitle } from '@Utils/usePageTitle'
import api from '@Api'

dayjs.extend(LocalizedFormat)

const Post: FC = () => {
  const { postId } = useParams()
  const navigate = useNavigate()

  const { classes, theme } = useBannerStyles()

  useEffect(() => {
    if (postId?.length != 8) {
      navigate('/404')
      return
    }
  }, [postId])

  const { data: post } = api.info.useInfoGetPost(
    postId ?? '',
    {
      refreshInterval: 0,
      revalidateIfStale: false,
      revalidateOnFocus: false,
    },
    postId?.length === 8
  )

  const { scrollIntoView, targetRef } = useScrollIntoView<HTMLDivElement>()
  useEffect(() => scrollIntoView({ alignment: 'center' }), [])

  usePageTitle(post?.title ?? 'Post')

  return (
    <WithNavBar width="100%" padding={0} isLoading={!post} minWidth={0}>
      <div ref={targetRef} className={classes.root}>
        <Stack
          spacing={6}
          align="center"
          style={{ width: '100%', padding: `0 ${theme.spacing.xs}px` }}
          className={classes.container}
        >
          <Title order={2} pb="1.5rem" className={classes.title} style={{ fontSize: 36 }}>
            {post?.title}
          </Title>
          <Avatar src={post?.autherAvatar} color="brand" radius="xl" size="lg">
            {post?.autherName?.slice(0, 1) ?? 'A'}
          </Avatar>
          <Text weight={700}>{post?.autherName ?? 'Anonym'}</Text>
          <Stack spacing={2}>
            <Divider color={theme.colorScheme === 'dark' ? 'white' : 'gray'} />
            <Text weight={500}>{dayjs(post?.time).format('LLL')}</Text>
          </Stack>
        </Stack>
      </div>
      <Container className={classes.content}>
        <MarkdownRender source={post?.content ?? ''} />
        {post?.tags && post.tags.length > 0 && (
          <Group position="right">
            {post.tags.map((tag, idx) => (
              <Text key={idx} weight={700} span color="brand">
                {`#${tag}`}
              </Text>
            ))}
          </Group>
        )}
        <Group spacing={5} mb={100} position="right">
          <Avatar src={post?.autherAvatar} size="sm">
            {post?.autherName?.slice(0, 1) ?? 'A'}
          </Avatar>
          <Text weight={700}>
            {post?.autherName ?? 'Anonym'} 发布于 {dayjs(post?.time).format('HH:mm, YY/MM/DD')}
          </Text>
        </Group>
      </Container>
    </WithNavBar>
  )
}

export default Post

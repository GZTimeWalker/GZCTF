import dayjs from 'dayjs'
import LocalizedFormat from 'dayjs/plugin/localizedFormat'
import { FC, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Avatar, Container, Divider, Stack, Title, Text, Group, Button } from '@mantine/core'
import { useScrollIntoView } from '@mantine/hooks'
import { mdiPencilOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import MarkdownRender from '@Components/MarkdownRender'
import WithNavBar from '@Components/WithNavbar'
import { RequireRole } from '@Components/WithRole'
import { useBannerStyles, useFixedButtonStyles } from '@Utils/ThemeOverride'
import { usePageTitle } from '@Utils/usePageTitle'
import { useUserRole } from '@Utils/useUser'
import api, { Role } from '@Api'

dayjs.extend(LocalizedFormat)

const Post: FC = () => {
  const { postId } = useParams()
  const navigate = useNavigate()

  const { classes, theme } = useBannerStyles()

  useEffect(() => {
    if (postId?.length !== 8) {
      navigate('/404')
      return
    }
  }, [postId])

  const { data: post } = api.info.useInfoGetPost(
    postId ?? '',
    {
      refreshInterval: 0,
      revalidateOnFocus: false,
    },
    postId?.length === 8
  )

  const { scrollIntoView, targetRef } = useScrollIntoView<HTMLDivElement>()
  useEffect(() => scrollIntoView({ alignment: 'center' }), [])

  const { classes: btnClasses } = useFixedButtonStyles({
    right: '2rem',
    bottom: '2rem',
  })

  const { role } = useUserRole()

  usePageTitle(post?.title ?? 'Post')

  return (
    <WithNavBar width="100%" isLoading={!post} minWidth={0} withFooter>
      <div ref={targetRef} className={classes.root}>
        <Stack
          spacing={6}
          align="center"
          w="100%"
          p={`0 ${theme.spacing.xs}`}
          className={classes.container}
        >
          <Title order={2} pb="1.5rem" className={classes.title} style={{ fontSize: 36 }}>
            {post?.title}
          </Title>
          <Avatar alt="avatar" src={post?.authorAvatar} color="brand" radius="xl" size="lg">
            {post?.authorName?.slice(0, 1) ?? 'A'}
          </Avatar>
          <Text fw={700}>{post?.authorName ?? 'Anonym'}</Text>
          <Stack spacing={2}>
            <Divider color={theme.colorScheme === 'dark' ? 'white' : 'gray'} />
            <Text fw={500}>{dayjs(post?.time).format('LLL')}</Text>
          </Stack>
        </Stack>
      </div>
      <Container className={classes.content}>
        <MarkdownRender source={post?.content ?? ''} />
        {post?.tags && post.tags.length > 0 && (
          <Group position="right">
            {post.tags.map((tag, idx) => (
              <Text key={idx} fw={700} span c="brand">
                {`#${tag}`}
              </Text>
            ))}
          </Group>
        )}
        <Group spacing={5} mb={100} position="right">
          <Avatar alt="avatar" src={post?.authorAvatar} size="sm">
            {post?.authorName?.slice(0, 1) ?? 'A'}
          </Avatar>
          <Text fw={700}>
            {post?.authorName ?? 'Anonym'} 发布于 {dayjs(post?.time).format('HH:mm, YY/MM/DD')}
          </Text>
        </Group>
      </Container>
      {RequireRole(Role.Admin, role) && (
        <Button
          className={btnClasses.fixedButton}
          variant="filled"
          radius="xl"
          size="md"
          leftIcon={<Icon path={mdiPencilOutline} size={1} />}
          onClick={() => navigate(`/posts/${postId}/edit`)}
        >
          编辑文章
        </Button>
      )}
    </WithNavBar>
  )
}

export default Post

import dayjs from 'dayjs'
import LocalizedFormat from 'dayjs/plugin/localizedFormat'
import { marked } from 'marked'
import { FC, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Avatar,
  Container,
  Divider,
  Stack,
  Title,
  Text,
  Group,
  TypographyStylesProvider,
} from '@mantine/core'
import WithNavBar from '@Components/WithNavbar'
import { usePageTitle } from '@Utils/usePageTitle'
import { useBannerStyles, useTypographyStyles } from '@Utils/ThemeOverride'
import api from '@Api'

dayjs.extend(LocalizedFormat)

const Post: FC = () => {
  const { postId } = useParams()
  const navigate = useNavigate()

  const { classes, theme } = useBannerStyles()
  const { classes: typographyClasses } = useTypographyStyles()

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

  usePageTitle(post?.title ?? 'Post')

  return (
    <WithNavBar width="100%" padding={0} isLoading={!post}>
      <div className={classes.root}>
        <Stack
          spacing={6}
          align="center"
          style={{ width: '100%', padding: `0 ${theme.spacing.md}px` }}
          className={classes.container}
        >
          <Title order={2} pb="2rem" className={classes.title} style={{ fontSize: 36 }}>
            {post?.title}
          </Title>
          <Avatar src={post?.autherAvatar} color="brand" radius="xl" size="md">
            {post?.autherName?.at(0) ?? 'A'}
          </Avatar>
          <Text weight={700}>{post?.autherName ?? 'Anonym'}</Text>
          <Stack spacing={2}>
            <Divider color="white" />
            <Group>
              <Text weight={500}>{dayjs(post?.time).format('LLL')}</Text>
            </Group>
          </Stack>
        </Stack>
      </div>
      <Container className={classes.content}>
        <TypographyStylesProvider className={typographyClasses.root}>
          <div dangerouslySetInnerHTML={{ __html: marked(post?.content ?? '') }} />
        </TypographyStylesProvider>
      </Container>
    </WithNavBar>
  )
}

export default Post

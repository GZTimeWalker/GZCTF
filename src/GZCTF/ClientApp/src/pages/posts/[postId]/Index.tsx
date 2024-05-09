import {
  Avatar,
  Button,
  Container,
  Divider,
  Group,
  Stack,
  Text,
  Title,
  useMantineColorScheme,
} from '@mantine/core'
import { useScrollIntoView } from '@mantine/hooks'
import { mdiPencilOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import MarkdownRender from '@Components/MarkdownRender'
import WithNavBar from '@Components/WithNavbar'
import { RequireRole } from '@Components/WithRole'
import { useLanguage } from '@Utils/I18n'
import { useBannerStyles, useFixedButtonStyles } from '@Utils/ThemeOverride'
import { usePageTitle } from '@Utils/usePageTitle'
import { useUserRole } from '@Utils/useUser'
import api, { Role } from '@Api'

const Post: FC = () => {
  const { postId } = useParams()
  const navigate = useNavigate()

  const { classes, theme } = useBannerStyles()

  const { t } = useTranslation()

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
  const { colorScheme } = useMantineColorScheme()
  const { locale } = useLanguage()

  usePageTitle(post?.title ?? 'Post')

  return (
    <WithNavBar width="100%" isLoading={!post} minWidth={0} withFooter>
      <div ref={targetRef} className={classes.root}>
        <Stack
          gap={6}
          align="center"
          w="100%"
          p={`0 ${theme.spacing.xs}`}
          className={classes.container}
        >
          <Title order={2} pb="1.5rem" className={classes.title} style={{ fontSize: 36 }}>
            {post?.title}
          </Title>
          <Avatar
            alt="avatar"
            src={post?.authorAvatar}
            color={theme.primaryColor}
            radius="xl"
            size="lg"
          >
            {post?.authorName?.slice(0, 1) ?? 'A'}
          </Avatar>
          <Text fw="bold">{post?.authorName ?? 'Anonym'}</Text>
          <Stack gap={2}>
            <Divider color={colorScheme === 'dark' ? 'white' : 'gray'} />
            <Text fw={500}>{dayjs(post?.time).locale(locale).format('lll')}</Text>
          </Stack>
        </Stack>
      </div>
      <Container className={classes.content}>
        <MarkdownRender source={post?.content ?? ''} />
        {post?.tags && post.tags.length > 0 && (
          <Group justify="right">
            {post.tags.map((tag, idx) => (
              <Text key={idx} fw="bold" span c={theme.primaryColor}>
                {`#${tag}`}
              </Text>
            ))}
          </Group>
        )}
        <Group gap={5} my="lg" justify="right">
          <Avatar alt="avatar" src={post?.authorAvatar} size="sm">
            {post?.authorName?.slice(0, 1) ?? 'A'}
          </Avatar>
          <Text fw="bold">
            {t('post.content.metadata', {
              author: post?.authorName ?? 'Anonym',
              date: dayjs(post?.time).locale(locale).format('LLL'),
            })}
          </Text>
        </Group>
      </Container>
      {RequireRole(Role.Admin, role) && (
        <Button
          className={btnClasses.fixedButton}
          variant="filled"
          radius="xl"
          size="md"
          leftSection={<Icon path={mdiPencilOutline} size={1} />}
          onClick={() => navigate(`/posts/${postId}/edit`)}
        >
          {t('post.button.edit')}
        </Button>
      )}
    </WithNavBar>
  )
}

export default Post

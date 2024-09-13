import {
  alpha,
  Button,
  Group,
  SimpleGrid,
  Stack,
  TagsInput,
  Text,
  Textarea,
  TextInput,
  Title,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiContentSaveOutline, mdiDeleteOutline, mdiFileCheckOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import WithNavBar from '@Components/WithNavbar'
import WithRole from '@Components/WithRole'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useIsMobile } from '@Utils/ThemeOverride'
import api, { PostEditModel, Role } from '@Api'

const PostEdit: FC = () => {
  const { postId } = useParams()
  const theme = useMantineTheme()
  const navigate = useNavigate()

  const { t } = useTranslation()

  useEffect(() => {
    if (postId?.length !== 8 && postId !== 'new') {
      navigate('/404')
      return
    }
  }, [postId])

  const { data: curPost } = api.info.useInfoGetPost(
    postId ?? '',
    {
      refreshInterval: 0,
      revalidateOnFocus: false,
      shouldRetryOnError: false,
    },
    postId?.length === 8
  )

  const [post, setPost] = useState<PostEditModel>({
    title: curPost?.title ?? '',
    content: curPost?.content ?? '',
    summary: curPost?.summary ?? '',
    isPinned: curPost?.isPinned ?? false,
    tags: curPost?.tags ?? [],
  })

  const [tags, setTags] = useState<string[]>([])
  const [disabled, setDisabled] = useState(false)
  const [hasChanged, setHasChanged] = useState(false)

  const modals = useModals()

  const isMobile = useIsMobile()
  const { colorScheme } = useMantineColorScheme()

  const onUpdate = () => {
    if (postId === 'new') {
      setDisabled(true)
      api.edit
        .editAddPost(post)
        .then((res) => {
          api.info.mutateInfoGetLatestPosts()
          api.info.mutateInfoGetPosts()
          showNotification({
            color: 'teal',
            message: t('post.notification.created'),
            icon: <Icon path={mdiCheck} size={24} />,
          })
          setHasChanged(false)
          navigate(`/posts/${res.data}/edit`)
        })
        .finally(() => {
          setDisabled(false)
        })
    } else if (postId?.length === 8) {
      setDisabled(true)
      api.edit
        .editUpdatePost(postId, post)
        .then((res) => {
          api.info.mutateInfoGetPost(postId, res.data)
          api.info.mutateInfoGetLatestPosts()
          api.info.mutateInfoGetPosts()
          showNotification({
            color: 'teal',
            message: t('post.notification.saved'),
            icon: <Icon path={mdiCheck} size={24} />,
          })
          setHasChanged(false)
        })
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  const onDelete = () => {
    if (!postId) return
    setDisabled(true)
    api.edit
      .editDeletePost(postId)
      .then(() => {
        api.info.mutateInfoGetPosts()
        api.info.mutateInfoGetLatestPosts()
        navigate('/posts')
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  useEffect(() => {
    if (!curPost) return

    setPost({
      title: curPost.title,
      content: curPost.content,
      summary: curPost.summary,
      isPinned: curPost.isPinned,
      tags: curPost.tags ?? [],
    })
    setTags(curPost.tags ?? [])
  }, [curPost])

  useEffect(() => {
    if (!curPost) return
    setHasChanged(
      post.title !== curPost.title ||
        post.content !== curPost.content ||
        post.summary !== curPost.summary ||
        post.isPinned !== curPost.isPinned ||
        (post.tags?.some((tag) => !curPost?.tags?.includes(tag)) ?? false)
    )
  }, [post, curPost])

  const titlePart = (
    <>
      <TextInput
        label={t('post.label.title')}
        value={post.title}
        onChange={(e) => setPost({ ...post, title: e.currentTarget.value })}
      />
      <TagsInput
        label={t('post.label.category')}
        data={tags.map((o) => ({ value: o, label: o })) || []}
        placeholder={t('post.label.add_tag')}
        value={post?.tags ?? []}
        onChange={(values) => setPost({ ...post, tags: values })}
        clearable
      />
    </>
  )

  return (
    <WithNavBar minWidth={0} withHeader stickyHeader>
      <WithRole requiredRole={Role.Admin}>
        <Stack mt={isMobile ? 25 : 30}>
          <Group justify={isMobile ? 'right' : 'space-between'}>
            {!isMobile && (
              <Title
                order={1}
                c={alpha(
                  colorScheme === 'dark' ? theme.colors.light[6] : theme.colors.gray[7],
                  0.5
                )}
              >
                {`> ${postId === 'new' ? t('post.button.new') : t('post.button.edit')}`}
              </Title>
            )}
            <Group justify="right">
              {postId?.length === 8 && (
                <>
                  <Button
                    disabled={disabled}
                    color="red"
                    leftSection={<Icon path={mdiDeleteOutline} size={1} />}
                    variant="outline"
                    onClick={() =>
                      modals.openConfirmModal({
                        title: t('post.button.delete'),
                        children: (
                          <Text size="sm">
                            {t('post.content.delete', {
                              title: curPost?.title,
                            })}
                          </Text>
                        ),
                        onConfirm: onDelete,
                        confirmProps: { color: 'red' },
                      })
                    }
                  >
                    {t('post.button.delete')}
                  </Button>
                  <Button
                    disabled={disabled}
                    leftSection={<Icon path={mdiFileCheckOutline} size={1} />}
                    onClick={() => {
                      if (hasChanged) {
                        modals.openConfirmModal({
                          title: t('post.content.updated.title'),
                          children: <Text size="sm">{t('post.content.updated.content')}</Text>,
                          onConfirm: () => {
                            onUpdate()
                            navigate(`/posts/${postId}`)
                          },
                        })
                      } else {
                        navigate(`/posts/${postId}`)
                      }
                    }}
                  >
                    {t('post.button.goto')}
                  </Button>
                </>
              )}
              <Button
                disabled={disabled}
                leftSection={<Icon path={mdiContentSaveOutline} size={1} />}
                onClick={onUpdate}
              >
                {postId === 'new' ? t('post.button.new') : t('post.button.save')}
              </Button>
            </Group>
          </Group>
          {isMobile ? titlePart : <SimpleGrid cols={2}>{titlePart}</SimpleGrid>}
          <Textarea
            label={
              <Group gap="sm">
                <Text size="sm">{t('post.label.summary')}</Text>
                <Text size="xs" c="dimmed">
                  {t('admin.content.markdown_support')}
                </Text>
              </Group>
            }
            autosize
            value={post.summary}
            onChange={(e) => setPost({ ...post, summary: e.currentTarget.value })}
            minRows={5}
            maxRows={5}
          />
          <Textarea
            label={
              <Group gap="sm">
                <Text size="sm">{t('post.label.content')}</Text>
                <Text size="xs" c="dimmed">
                  {t('admin.content.markdown_support')}
                </Text>
              </Group>
            }
            autosize
            value={post.content}
            onChange={(e) => setPost({ ...post, content: e.currentTarget.value })}
            minRows={isMobile ? 14 : 16}
            maxRows={isMobile ? 14 : 16}
          />
        </Stack>
      </WithRole>
    </WithNavBar>
  )
}

export default PostEdit

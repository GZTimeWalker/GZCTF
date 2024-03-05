import {
  Button,
  Group,
  MultiSelect,
  Stack,
  Text,
  Textarea,
  TextInput,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiContentSaveOutline, mdiDeleteOutline, mdiFileCheckOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router'
import StickyHeader from '@Components/StickyHeader'
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
  const modals = useModals()

  const isMobile = useIsMobile()

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

  const isChanged = () =>
    post.title !== curPost?.title ||
    post.content !== curPost?.content ||
    post.summary !== curPost?.summary ||
    (post.tags?.some((tag) => !curPost?.tags?.includes(tag)) ?? false)

  const titlePart = (
    <>
      <TextInput
        label={t('post.label.title')}
        value={post.title}
        onChange={(e) => setPost({ ...post, title: e.currentTarget.value })}
      />
      <MultiSelect
        label={t('post.label.tag')}
        data={tags.map((o) => ({ value: o, label: o })) || []}
        getCreateLabel={(query) => t('post.label.add_tag', { query })}
        maxSelectedValues={5}
        value={post?.tags ?? []}
        onChange={(values) => setPost({ ...post, tags: values })}
        onCreate={(query) => {
          const item = { value: query, label: query }
          setTags([...tags, query])
          return item
        }}
        searchable
        creatable
      />
    </>
  )

  return (
    <WithNavBar minWidth={0}>
      <WithRole requiredRole={Role.Admin}>
        <StickyHeader />
        <Stack mt={isMobile ? 5 : 30}>
          <Group position={isMobile ? 'right' : 'apart'}>
            {!isMobile && (
              <Title
                order={1}
                color={theme.fn.rgba(
                  theme.colorScheme === 'dark' ? theme.colors.white[6] : theme.colors.gray[7],
                  0.5
                )}
              >
                {`> ${postId === 'new' ? t('post.button.new') : t('post.button.edit')}`}
              </Title>
            )}
            <Group position="right">
              {postId?.length === 8 && (
                <>
                  <Button
                    disabled={disabled}
                    color="red"
                    leftIcon={<Icon path={mdiDeleteOutline} size={1} />}
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
                    leftIcon={<Icon path={mdiFileCheckOutline} size={1} />}
                    onClick={() => {
                      if (isChanged()) {
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
                leftIcon={<Icon path={mdiContentSaveOutline} size={1} />}
                onClick={onUpdate}
              >
                {postId === 'new' ? t('post.button.new') : t('post.button.save')}
              </Button>
            </Group>
          </Group>
          {isMobile ? titlePart : <Group grow>{titlePart}</Group>}
          <Textarea
            label={
              <Group spacing="sm">
                <Text size="sm">{t('post.label.summary')}</Text>
                <Text size="xs" c="dimmed">
                  {t('admin.content.markdown_support')}
                </Text>
              </Group>
            }
            value={post.summary}
            onChange={(e) => setPost({ ...post, summary: e.currentTarget.value })}
            minRows={3}
            maxRows={3}
          />
          <Textarea
            label={
              <Group spacing="sm">
                <Text size="sm">{t('post.label.content')}</Text>
                <Text size="xs" c="dimmed">
                  {t('admin.content.markdown_support')}
                </Text>
              </Group>
            }
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

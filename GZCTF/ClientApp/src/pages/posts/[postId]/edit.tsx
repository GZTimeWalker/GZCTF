import { FC, useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router'
import {
  Stack,
  TextInput,
  Title,
  Textarea,
  MultiSelect,
  Group,
  Text,
  Button,
  useMantineTheme,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiContentSaveOutline, mdiDeleteOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import StickyHeader from '@Components/StickyHeader'
import WithNavBar from '@Components/WithNavbar'
import WithRole from '@Components/WithRole'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useIsMobile } from '@Utils/ThemeOverride'
import api, { PostEditModel, Role } from '@Api'

const PostEdit: FC = () => {
  const { postId } = useParams()
  const theme = useMantineTheme()
  const navigate = useNavigate()

  useEffect(() => {
    if (postId?.length != 8 && postId !== 'new') {
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
    tags: curPost?.tags ?? [],
  })

  const [tags, setTags] = useState<string[]>([])
  const [disabled, setDisabled] = useState(false)
  const modals = useModals()

  const { isMobile } = useIsMobile()

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
            message: '文章已创建',
            icon: <Icon path={mdiCheck} size={24} />,
            disallowClose: true,
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
            message: '文章已保存',
            icon: <Icon path={mdiCheck} size={24} />,
            disallowClose: true,
          })
        })
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  const onDelete = () => {
    if (postId) {
      setDisabled(true)
      api.edit
        .editDeletePost(postId)
        .then(() => {
          api.info.mutateInfoGetPosts()
          api.info.mutateInfoGetLatestPosts()
          navigate('/posts')
        })
        .catch(showErrorNotification)
        .finally(() => {
          setDisabled(false)
        })
    }
  }

  useEffect(() => {
    if (curPost) {
      setPost({
        title: curPost.title,
        content: curPost.content,
        summary: curPost.summary,
        isPinned: curPost.isPinned,
        tags: curPost.tags ?? [],
      })
      setTags(curPost.tags ?? [])
    }
  }, [curPost])

  const titlePart = (
    <>
      <TextInput
        label="文章标题"
        value={post.title}
        onChange={(e) => setPost({ ...post, title: e.currentTarget.value })}
      />
      <MultiSelect
        label="文章标签"
        data={tags.map((o) => ({ value: o, label: o })) || []}
        getCreateLabel={(query) => `+ 添加标签 "${query}"`}
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
                style={{
                  color: theme.fn.rgba(
                    theme.colorScheme === 'dark' ? theme.colors.white[6] : theme.colors.gray[7],
                    0.5
                  ),
                }}
              >
                {`> ${postId === 'new' ? '新建' : '编辑'}文章`}
              </Title>
            )}
            <Group position="right">
              {postId?.length === 8 && (
                <Button
                  disabled={disabled}
                  color="red"
                  leftIcon={<Icon path={mdiDeleteOutline} size={1} />}
                  variant="outline"
                  onClick={() =>
                    modals.openConfirmModal({
                      title: '删除文章',
                      children: <Text size="sm">你确定要删除文章 "{post.title}" 吗？</Text>,
                      centered: true,
                      onConfirm: onDelete,
                      labels: { confirm: '确认', cancel: '取消' },
                      confirmProps: { color: 'red' },
                    })
                  }
                >
                  删除文章
                </Button>
              )}
              <Button
                disabled={disabled}
                leftIcon={<Icon path={mdiContentSaveOutline} size={1} />}
                onClick={onUpdate}
              >
                {`${postId === 'new' ? '创建' : '保存'}文章`}
              </Button>
            </Group>
          </Group>
          {isMobile ? titlePart : <Group grow>titlePart</Group>}
          <Textarea
            label={
              <Group spacing="sm">
                <Text size="sm">文章梗概</Text>
                <Text size="xs" color="dimmed">
                  支持 markdown 语法
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
                <Text size="sm">文章内容</Text>
                <Text size="xs" color="dimmed">
                  支持 markdown 语法
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

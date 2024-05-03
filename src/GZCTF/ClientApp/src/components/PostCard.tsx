import {
  ActionIcon,
  Anchor,
  Avatar,
  Card,
  Group,
  Stack,
  Text,
  Title,
  useMantineColorScheme,
  useMantineTheme,
} from '@mantine/core'
import { mdiFormatQuoteOpen, mdiPencilOutline, mdiPinOffOutline, mdiPinOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import MarkdownRender from '@Components/MarkdownRender'
import { RequireRole } from '@Components/WithRole'
import { useUserRole } from '@Utils/useUser'
import { PostInfoModel, Role } from '@Api'

export interface PostCardProps {
  post: PostInfoModel
  onTogglePinned?: (post: PostInfoModel, setDisabled: (value: boolean) => void) => void
}

const PostCard: FC<PostCardProps> = ({ post, onTogglePinned }) => {
  const navigate = useNavigate()
  const { role } = useUserRole()
  const [disabled, setDisabled] = useState(false)
  const { t } = useTranslation()
  const theme = useMantineTheme()
  const { colorScheme } = useMantineColorScheme()

  return (
    <Card shadow="sm" p="md">
      <Group px="sm" py="xs" wrap="nowrap" justify="space-between" align="flex-start">
        <Icon
          path={mdiFormatQuoteOpen}
          size={1.5}
          color={theme.colors[theme.primaryColor][colorScheme === 'dark' ? 7 : 5]}
        />
        <Stack gap="xs" w="calc(100% - 3rem)">
          {RequireRole(Role.Admin, role) ? (
            <Group justify="space-between">
              <Title order={3}>
                {post.isPinned && (
                  <Text span c="brand">
                    {`${t('post.content.pinned')} `}
                  </Text>
                )}
                {post.title}
              </Title>
              <Group justify="right">
                {onTogglePinned && (
                  <ActionIcon disabled={disabled} onClick={() => onTogglePinned(post, setDisabled)}>
                    {post.isPinned ? (
                      <Icon path={mdiPinOffOutline} size={1} />
                    ) : (
                      <Icon path={mdiPinOutline} size={1} />
                    )}
                  </ActionIcon>
                )}
                <ActionIcon onClick={() => navigate(`/posts/${post.id}/edit`)}>
                  <Icon path={mdiPencilOutline} size={1} />
                </ActionIcon>
              </Group>
            </Group>
          ) : (
            <Title order={3}>
              {post.isPinned && (
                <Text span c="brand">
                  {`${t('post.content.pinned')} `}
                </Text>
              )}
              {post.title}
            </Title>
          )}
          <MarkdownRender source={post.summary} />
          {post.tags && (
            <Group>
              {post.tags.map((tag, idx) => (
                <Text key={idx} size="sm" fw="bold" span c="brand">
                  {`#${tag}`}
                </Text>
              ))}
            </Group>
          )}
          <Group pt="xs" w="100%" justify="space-between" m="auto" fs="normal">
            <Group gap={5} justify="right">
              <Avatar alt="avatar" src={post.authorAvatar} size="sm">
                {post.authorName?.slice(0, 1) ?? 'A'}
              </Avatar>
              <Text size="sm" fw="bold" c="dimmed">
                {t('post.content.metadata', {
                  author: post.authorName ?? 'Anonym',
                  date: dayjs(post.time).format('HH:mm, YY/MM/DD'),
                })}
              </Text>
            </Group>
            <Text ta="right">
              <Anchor component={Link} to={`/posts/${post.id}`}>
                <Text span fw="bold" size="sm">
                  {t('post.content.details')} &gt;&gt;&gt;
                </Text>
              </Anchor>
            </Text>
          </Group>
        </Stack>
      </Group>
    </Card>
  )
}

export default PostCard

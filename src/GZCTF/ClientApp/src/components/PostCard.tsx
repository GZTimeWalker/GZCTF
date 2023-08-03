import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  Group,
  Card,
  Blockquote,
  Title,
  Text,
  Stack,
  Avatar,
  Anchor,
  ActionIcon,
  useMantineTheme,
} from '@mantine/core'
import { mdiPencilOutline, mdiPinOffOutline, mdiPinOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { useUserRole } from '@Utils/useUser'
import { PostInfoModel, Role } from '@Api'
import MarkdownRender from './MarkdownRender'
import { RequireRole } from './WithRole'

export interface PostCardProps {
  post: PostInfoModel
  onTogglePinned?: (post: PostInfoModel, setDisabled: (value: boolean) => void) => void
}

const PostCard: FC<PostCardProps> = ({ post, onTogglePinned }) => {
  const theme = useMantineTheme()
  const navigate = useNavigate()
  const { role } = useUserRole()
  const [disabled, setDisabled] = useState(false)

  return (
    <Card shadow="sm" p="xs">
      <Blockquote
        color={theme.colorScheme === 'dark' ? theme.colors.dark[1] : theme.colors.gray[7]}
        cite={
          <Group position="apart" m="auto" fs="normal">
            <Group spacing={5} position="right">
              <Avatar alt="avatar" src={post.authorAvatar} size="sm">
                {post.authorName?.slice(0, 1) ?? 'A'}
              </Avatar>
              <Text fw={700}>
                {post.authorName ?? 'Anonym'} 发布于 {dayjs(post.time).format('HH:mm, YY/MM/DD')}
              </Text>
            </Group>
            <Text align="right">
              <Anchor component={Link} to={`/posts/${post.id}`}>
                <Text span fw={500} size="sm">
                  查看详情 &gt;&gt;&gt;
                </Text>
              </Anchor>
            </Text>
          </Group>
        }
      >
        <Stack spacing="xs">
          {RequireRole(Role.Admin, role) ? (
            <Group position="apart">
              <Title order={3}>
                {post.isPinned && (
                  <Text span c="brand">
                    {'[置顶] '}
                  </Text>
                )}
                {post.title}
              </Title>
              <Group position="right">
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
                  {'[置顶] '}
                </Text>
              )}
              {post.title}
            </Title>
          )}
          <MarkdownRender source={post.summary} />
          {post.tags && (
            <Group>
              {post.tags.map((tag, idx) => (
                <Text key={idx} size="sm" fw={700} span c="brand">
                  {`#${tag}`}
                </Text>
              ))}
            </Group>
          )}
        </Stack>
      </Blockquote>
    </Card>
  )
}

export default PostCard

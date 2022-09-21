import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Group, Card, Title, Text, Stack, ActionIcon, Box, Avatar } from '@mantine/core'
import { mdiPencilOutline, mdiPinOffOutline, mdiPinOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { useUserRole } from '@Utils/useUser'
import { Role } from '@Api'
import MarkdownRender from './MarkdownRender'
import { PostCardProps } from './PostCard'
import { RequireRole } from './WithRole'

const MobilePostCard: FC<PostCardProps> = ({ post, onTogglePinned }) => {
  const navigate = useNavigate()
  const { role } = useUserRole()
  const [disabled, setDisabled] = useState(false)

  return (
    <Card shadow="sm" p="sm">
      <Stack spacing="xs">
        <Box onClick={() => navigate(`/posts/${post.id}`)}>
          <Title order={3} pb={4}>
            <Text span color="brand">
              {post.isPinned ? '[置顶] ' : '>>> '}
            </Text>
            {post.title}
          </Title>
          <MarkdownRender source={post.summary} />
        </Box>
        <Group position="apart">
          {post.tags && (
            <Group position="left">
              {post.tags.map((tag, idx) => (
                <Text key={idx} size="sm" weight={700} span color="brand">
                  {`#${tag}`}
                </Text>
              ))}
            </Group>
          )}
          {RequireRole(Role.Admin, role) && (
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
          )}
        </Group>
        <Group spacing={5} position="left">
          <Avatar src={post.autherAvatar} size="sm">
            {post.autherName?.slice(0, 1) ?? 'A'}
          </Avatar>
          <Text weight={500} size="sm">
            {post.autherName ?? 'Anonym'} 发布于 {dayjs(post.time).format('HH:mm, YY/MM/DD')}
          </Text>
        </Group>
      </Stack>
    </Card>
  )
}

export default MobilePostCard

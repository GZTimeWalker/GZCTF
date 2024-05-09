import {
  ActionIcon,
  Avatar,
  Box,
  Card,
  Group,
  Stack,
  Text,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { mdiPencilOutline, mdiPinOffOutline, mdiPinOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import MarkdownRender from '@Components/MarkdownRender'
import { PostCardProps } from '@Components/PostCard'
import { RequireRole } from '@Components/WithRole'
import { useUserRole } from '@Utils/useUser'
import { Role } from '@Api'

const MobilePostCard: FC<PostCardProps> = ({ post, onTogglePinned }) => {
  const navigate = useNavigate()
  const { role } = useUserRole()
  const [disabled, setDisabled] = useState(false)

  const { t } = useTranslation()
  const theme = useMantineTheme()

  return (
    <Card shadow="sm" p="sm">
      <Stack gap="xs">
        <Box onClick={() => navigate(`/posts/${post.id}`)}>
          <Title order={3} pb={4}>
            <Text fw="bold" fz="h3" span c={theme.primaryColor}>
              {post.isPinned ? `${t('post.content.pinned')} ` : '>>> '}
            </Text>
            {post.title}
          </Title>
          <MarkdownRender source={post.summary} />
        </Box>
        <Group justify="space-between">
          {post.tags && (
            <Group justify="left">
              {post.tags.map((tag, idx) => (
                <Text key={idx} size="sm" fw="bold" span c={theme.primaryColor}>
                  {`#${tag}`}
                </Text>
              ))}
            </Group>
          )}
          {RequireRole(Role.Admin, role) && (
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
          )}
        </Group>
        <Group gap={5} justify="left">
          <Avatar alt="avatar" src={post.authorAvatar} size="sm">
            {post.authorName?.slice(0, 1) ?? 'A'}
          </Avatar>
          <Text fw={500} size="sm">
            {t('post.content.metadata', {
              author: post.authorName ?? 'Anonym',
              date: dayjs(post.time).format('HH:mm, YY/MM/DD'),
            })}
          </Text>
        </Group>
      </Stack>
    </Card>
  )
}

export default MobilePostCard

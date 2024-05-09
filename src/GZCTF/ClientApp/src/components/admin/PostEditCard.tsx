import {
  ActionIcon,
  Badge,
  Card,
  CardProps,
  Group,
  Stack,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { mdiDeleteOutline, mdiPencilOutline, mdiPinOffOutline, mdiPinOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC } from 'react'
import MarkdownRender from '@Components/MarkdownRender'
import { PostInfoModel } from '@Api'

interface PostEditCardProps extends CardProps {
  post: PostInfoModel
  onDelete: () => void
  onEdit: () => void
  onPin: () => void
}

const PostEditCard: FC<PostEditCardProps> = ({ post, onDelete, onEdit, onPin, ...props }) => {
  const theme = useMantineTheme()
  return (
    <Card {...props} shadow="sm" p="lg">
      <Stack>
        <Group justify="space-between">
          <Group justify="left">
            {post.isPinned && <Icon color={theme.colors.orange[4]} path={mdiPinOutline} size={1} />}
            <Title order={3}>{post.title}</Title>
          </Group>
          <Group justify="right">
            <ActionIcon onClick={onPin}>
              {post.isPinned ? (
                <Icon path={mdiPinOffOutline} size={1} />
              ) : (
                <Icon path={mdiPinOutline} size={1} />
              )}
            </ActionIcon>
            <ActionIcon onClick={onEdit}>
              <Icon path={mdiPencilOutline} size={1} />
            </ActionIcon>
            <ActionIcon onClick={onDelete} color="alert">
              <Icon path={mdiDeleteOutline} size={1} />
            </ActionIcon>
          </Group>
        </Group>
        <MarkdownRender source={post.summary} />
        <Group justify="right">
          <Badge color={theme.primaryColor} variant="light">
            {new Date(post.time).toLocaleString()}
          </Badge>
        </Group>
      </Stack>
    </Card>
  )
}

export default PostEditCard

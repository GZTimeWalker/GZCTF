import { FC } from 'react'
import {
  ActionIcon,
  Group,
  Stack,
  Badge,
  Card,
  Title,
  PaperProps,
  useMantineTheme,
} from '@mantine/core'
import { mdiPinOffOutline, mdiPinOutline, mdiDeleteOutline, mdiPencilOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import MarkdownRender from '@Components/MarkdownRender'
import { PostInfoModel } from '@Api'

interface PostEditCardProps extends PaperProps {
  post: PostInfoModel
  onDelete: () => void
  onEdit: () => void
  onPin: () => void
}

const PostEditCard: FC<PostEditCardProps> = ({ post, onDelete, onEdit, onPin, ...props }) => {
  const theme = useMantineTheme()
  return (
    <Card shadow="sm" p="lg" {...props}>
      <Stack>
        <Group position="apart">
          <Group position="left">
            {post.isPinned && (
              <Icon color={theme.colors.orange[4]} path={mdiPinOutline} size={1} />
            )}
            <Title order={3}>{post.title}</Title>
          </Group>
          <Group position="right">
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
        <Group position="right">
          <Badge color="brand" variant="light">
            {new Date(post.time).toLocaleString()}
          </Badge>
        </Group>
      </Stack>
    </Card>
  )
}

export default PostEditCard

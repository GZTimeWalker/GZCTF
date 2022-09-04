import { marked } from 'marked'
import { FC } from 'react'
import {
  ActionIcon,
  Group,
  Stack,
  Badge,
  Card,
  Title,
  TypographyStylesProvider,
  PaperProps,
} from '@mantine/core'
import { mdiPinOffOutline, mdiPinOutline, mdiDeleteOutline, mdiPencilOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { useTypographyStyles } from '@Utils/ThemeOverride'
import { PostInfoModel } from '@Api'

interface PostEditCardProps extends PaperProps {
  post: PostInfoModel
  onDelete: () => void
  onEdit: () => void
  onPin: () => void
}

const PostEditCard: FC<PostEditCardProps> = ({ post, onDelete, onEdit, onPin, ...props }) => {
  const { classes, theme } = useTypographyStyles()
  return (
    <Card shadow="sm" p="lg" {...props}>
      <Stack>
        <Group position="apart">
          <Group position="left">
            {post.isPinned && (
              <Icon color={theme.colors.orange[4]} path={mdiPinOutline} size={1}></Icon>
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
        <TypographyStylesProvider className={classes.root}>
          <div dangerouslySetInnerHTML={{ __html: marked(post.summary) }} />
        </TypographyStylesProvider>
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

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
import { Notice } from '@Api/Api'

interface NoticeEditCardProps extends PaperProps {
  notice: Notice
  onDelete: () => void
  onEdit: () => void
  onPin: () => void
}

const NoticeEditCard: FC<NoticeEditCardProps> = ({ notice, onDelete, onEdit, onPin, ...props }) => {
  const { classes, theme } = useTypographyStyles()
  return (
    <Card shadow="sm" p="lg" {...props}>
      <Stack>
        <Group position="apart">
          <Group position="left">
            {notice.isPinned && (
              <Icon color={theme.colors.orange[4]} path={mdiPinOutline} size={1}></Icon>
            )}
            <Title order={3}>{notice.title}</Title>
          </Group>
          <Group position="right">
            <ActionIcon onClick={onPin}>
              {notice.isPinned ? (
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
          <div dangerouslySetInnerHTML={{ __html: marked(notice.content) }} />
        </TypographyStylesProvider>
        <Group position="right">
          <Badge color="brand" variant="light">
            {new Date(notice.time).toLocaleString()}
          </Badge>
        </Group>
      </Stack>
    </Card>
  )
}

export default NoticeEditCard

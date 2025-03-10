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
import { Link } from 'react-router'
import { Markdown } from '@Components/MarkdownRenderer'
import { RequireRole } from '@Components/WithRole'
import { useLanguage } from '@Utils/I18n'
import { useUserRole } from '@Hooks/useUser'
import { PostInfoModel, Role } from '@Api'

export interface PostCardProps {
  post: PostInfoModel
  onTogglePinned?: (post: PostInfoModel, setDisabled: (value: boolean) => void) => void
}

export const PostCard: FC<PostCardProps> = ({ post, onTogglePinned }) => {
  const theme = useMantineTheme()
  const { role } = useUserRole()
  const { t } = useTranslation()
  const { colorScheme } = useMantineColorScheme()
  const [disabled, setDisabled] = useState(false)

  const { locale } = useLanguage()

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
            <Group justify="space-between" wrap="nowrap">
              <Title order={3}>
                {post.isPinned && (
                  <Text fw="bold" fz="h3" span c={theme.primaryColor}>
                    {t('post.content.pinned')}&nbsp;&nbsp;
                  </Text>
                )}
                {post.title}
              </Title>
              <Group justify="right">
                {onTogglePinned && (
                  <ActionIcon disabled={disabled} onClick={() => onTogglePinned(post, setDisabled)}>
                    {post.isPinned ? <Icon path={mdiPinOffOutline} size={1} /> : <Icon path={mdiPinOutline} size={1} />}
                  </ActionIcon>
                )}
                <ActionIcon component={Link} to={`/posts/${post.id}/edit`}>
                  <Icon path={mdiPencilOutline} size={1} />
                </ActionIcon>
              </Group>
            </Group>
          ) : (
            <Title order={3}>
              {post.isPinned && (
                <Text fw="bold" fz="h3" span c={theme.primaryColor}>
                  {`${t('post.content.pinned')} `}
                </Text>
              )}
              {post.title}
            </Title>
          )}
          <Markdown source={post.summary} />
          {post.tags && (
            <Group>
              {post.tags.map((tag, idx) => (
                <Text key={idx} size="sm" fw="bold" span c={theme.primaryColor}>
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
                  date: dayjs(post.time).locale(locale).format('LLL'),
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

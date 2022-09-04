import dayjs from 'dayjs'
import { marked } from 'marked'
import { FC } from 'react'
import { Link } from 'react-router-dom'
import {
  Group,
  Card,
  Blockquote,
  Title,
  Text,
  Stack,
  TypographyStylesProvider,
  Avatar,
  Anchor,
} from '@mantine/core'
import { useTypographyStyles } from '@Utils/ThemeOverride'
import { PostInfoModel } from '@Api'

const PostCard: FC<PostInfoModel> = (post) => {
  const { classes, theme } = useTypographyStyles()

  return (
    <Card shadow="sm" p="xs">
      <Blockquote
        color={theme.colorScheme === 'dark' ? theme.colors.dark[1] : theme.colors.gray[7]}
        cite={
          <Group position="apart" style={{ margin: 'auto', fontStyle: 'normal' }}>
            <Group spacing={5} position="right">
              <Avatar src={post.autherAvatar} size="sm">
                {post.autherName?.at(0) ?? 'A'}
              </Avatar>
              <Text weight={700}>
                {post.autherName ?? 'Anonym'} 发布于 {dayjs(post.time).format('HH:mm, YY/MM/DD')}
              </Text>
            </Group>
            <Text align="right">
              <Anchor component={Link} to={`/posts/${post.id}`}>
                <Text span weight={500} size="sm">
                  查看详情 &gt;&gt;&gt;
                </Text>
              </Anchor>
            </Text>
          </Group>
        }
      >
        <Stack spacing="xs">
          <Title order={3}>
            {post.isPinned && (
              <Text span color="brand">
                {'[置顶] '}
              </Text>
            )}
            {post.title}
          </Title>
          <TypographyStylesProvider className={classes.root}>
            <div dangerouslySetInnerHTML={{ __html: marked(post.summary) }} />
          </TypographyStylesProvider>
        </Stack>
      </Blockquote>
    </Card>
  )
}

export default PostCard

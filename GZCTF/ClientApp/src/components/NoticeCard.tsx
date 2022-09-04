import { marked } from 'marked'
import { FC } from 'react'
import {
  Group,
  Badge,
  Card,
  Blockquote,
  Title,
  Stack,
  TypographyStylesProvider,
} from '@mantine/core'
import { useTypographyStyles } from '@Utils/ThemeOverride'
import { Notice } from '@Api'

const NoticeCard: FC<Notice> = (notice) => {
  const { classes, theme } = useTypographyStyles()

  return (
    <Card shadow="sm" p="lg">
      <Blockquote
        color={theme.colorScheme === 'dark' ? theme.colors.dark[1] : theme.colors.gray[7]}
        cite={
          <Group position="right" style={{ margin: 'auto', fontStyle: 'normal' }}>
            <Badge color="brand" variant="light">
              {new Date(notice.time).toLocaleString()}
            </Badge>
          </Group>
        }
      >
        <Stack>
          <Title order={3}>{notice.title}</Title>
          <TypographyStylesProvider className={classes.root}>
            <div dangerouslySetInnerHTML={{ __html: marked(notice.content) }} />
          </TypographyStylesProvider>
        </Stack>
      </Blockquote>
    </Card>
  )
}

export default NoticeCard

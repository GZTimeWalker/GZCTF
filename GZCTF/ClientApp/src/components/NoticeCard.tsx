import { marked } from 'marked'
import { FC } from 'react'
import {
  Group,
  Badge,
  Card,
  useMantineTheme,
  Blockquote,
  Title,
  Stack,
  TypographyStylesProvider,
} from '@mantine/core'
import { Notice } from '../Api'

const NoticeCard: FC<Notice> = (notice) => {
  const theme = useMantineTheme()
  const secondaryColor = theme.colorScheme === 'dark' ? theme.colors.dark[1] : theme.colors.gray[7]

  return (
    <Card shadow="sm" p="lg">
      <Blockquote
        color={secondaryColor}
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
          <TypographyStylesProvider>
            <div dangerouslySetInnerHTML={{ __html: marked(notice.content) }} />
          </TypographyStylesProvider>
        </Stack>
      </Blockquote>
    </Card>
  )
}

export default NoticeCard

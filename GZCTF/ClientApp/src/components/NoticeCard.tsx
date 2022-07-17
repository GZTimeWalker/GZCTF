import { FC } from 'react';
import { Group, Text, Badge, Card, useMantineTheme, Blockquote, Title, Stack } from '@mantine/core';
import { Notice } from '../Api';

const NoticeCard: FC<Notice> = (notice) => {
  const theme = useMantineTheme();
  const secondaryColor = theme.colorScheme === 'dark' ? theme.colors.dark[1] : theme.colors.gray[7];

  return (
    <Card shadow="sm" p="lg" style={{ width: '80%' }}>
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
          <Text size="sm" style={{ color: secondaryColor, lineHeight: 1.5 }}>
            {notice.content}
          </Text>
        </Stack>
      </Blockquote>
    </Card>
  );
};

export default NoticeCard;

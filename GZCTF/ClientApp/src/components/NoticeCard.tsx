import { FC } from 'react';
import { Group, Text, Badge, Card, useMantineTheme } from '@mantine/core';
import { Notice } from '../Api';

const NoticeCard: FC<Notice> = (notice) => {
  const theme = useMantineTheme();
  const secondaryColor = theme.colorScheme === 'dark' ? theme.colors.dark[1] : theme.colors.gray[7];

  return (
    <Card shadow="sm" p="lg" style={{ width: '80%' }}>
      <Group position="apart" style={{ margin: 'auto' }}>
        <Text weight={500}>{notice.title}</Text>
        <Badge color="brand" variant="light">
          {new Date(notice.time).toLocaleString()}
        </Badge>
      </Group>
      <Text size="sm" style={{ color: secondaryColor, lineHeight: 1.5 }}>
        {notice.content}
      </Text>
    </Card>
  );
};

export default NoticeCard;

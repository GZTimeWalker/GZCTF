import { FC, useState } from 'react';
import {
  ActionIcon,
  Group,
  Stack,
  Title,
  Text,
  Badge,
  Card,
  useMantineTheme,
  TypographyStylesProvider,
} from '@mantine/core';
import { mdiPinOffOutline, mdiPinOutline, mdiDeleteOutline, mdiFileEditOutline } from '@mdi/js';
import Icon from '@mdi/react';
import api, { Notice } from '../../Api';
import NoticeEditModal from './edit/NoticeEditModal';
import { marked } from 'marked';

interface NoticeEditCardProps {
  notice: Notice;
  onDelete: () => void;
  onEdit: () => void;
  onPin: () => void;
}

const NoticeEditCard: FC<NoticeEditCardProps> = ({ notice, onDelete, onEdit, onPin }) => {
  const theme = useMantineTheme();
  return (
    <Card shadow="sm" p="lg">
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
              <Icon path={mdiFileEditOutline} size={1} />
            </ActionIcon>
            <ActionIcon onClick={onDelete} color="red">
              <Icon path={mdiDeleteOutline} size={1} />
            </ActionIcon>
          </Group>
        </Group>
        <TypographyStylesProvider>
          <div dangerouslySetInnerHTML={{ __html: marked(notice.content) }} />
        </TypographyStylesProvider>
        <Group position="right">
          <Badge color="brand" variant="light">
            {new Date(notice.time).toLocaleString()}
          </Badge>
        </Group>
      </Stack>
    </Card>
  );
};

const NoticeManager: FC = () => {
  const {
    data: notices,
    error,
    mutate,
  } = api.edit.useEditGetNotices({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  });

  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [activeNotice, setActiveNotice] = useState<Notice | null>(null);

  const onPin = (notice: Notice) => {
    api.edit.editUpdateNotice(notice.id!, { ...notice, isPinned: !notice.isPinned }).then(() => {
      mutate();
    });
  };

  return (
    <Stack
      spacing="lg"
      style={{
        margin: '2%',
      }}
    >
      {notices &&
        notices.map((notice) => (
          <NoticeEditCard
            key={notice.id}
            notice={notice}
            onEdit={() => {
              setActiveNotice(notice);
              setIsEditModalOpen(true);
            }}
            onDelete={() => {}}
            onPin={() => onPin(notice)}
          />
        ))}

      <NoticeEditModal
        centered
        size='30%'
        title={activeNotice ? '编辑通知' : '新建通知'}
        notice={activeNotice}
        opened={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        mutateNotice={
          (notice: Notice) => {
            mutate([notice, ...notices?.filter((n) => n.id !== notice.id) ?? []]);
          }
        }
      />
    </Stack>
  );
};

export default NoticeManager;

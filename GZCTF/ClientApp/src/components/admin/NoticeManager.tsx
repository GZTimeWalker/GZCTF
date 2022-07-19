import { marked } from 'marked';
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
  Button,
  Center,
  Modal,
} from '@mantine/core';
import { showNotification } from '@mantine/notifications';
import {
  mdiPinOffOutline,
  mdiPinOutline,
  mdiDeleteOutline,
  mdiFileEditOutline,
  mdiPlus,
  mdiCheck,
  mdiClose,
} from '@mdi/js';
import Icon from '@mdi/react';
import api, { Notice } from '../../Api';
import NoticeEditModal from './edit/NoticeEditModal';

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
  const { data: notices, mutate } = api.edit.useEditGetNotices({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  });

  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [activeNotice, setActiveNotice] = useState<Notice | null>(null);

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [deleteNotice, setDeleteNotice] = useState<Notice | null>(null);

  const onPin = (notice: Notice) => {
    api.edit.editUpdateNotice(notice.id!, { ...notice, isPinned: !notice.isPinned }).then(() => {
      mutate();
    });
  };

  const onConfirmDelete = () => {
    if (!deleteNotice) return;

    api.edit
      .editDeleteNotice(deleteNotice.id!)
      .then(() => {
        showNotification({
          color: 'teal',
          message: '通知已删除',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        });
        setDeleteNotice(null);
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        });
      })
      .finally(() => {
        setIsDeleteModalOpen(false);
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
        notices
          .sort((x, y) => (x.isPinned || new Date(x.time) < new Date(y.time) ? 1 : -1))
          .map((notice) => (
            <NoticeEditCard
              key={notice.id}
              notice={notice}
              onEdit={() => {
                setActiveNotice(notice);
                setIsEditModalOpen(true);
              }}
              onDelete={() => {
                setDeleteNotice(notice);
                setIsDeleteModalOpen(true);
              }}
              onPin={() => onPin(notice)}
            />
          ))}

      <Center>
        <Button
          leftIcon={<Icon path={mdiPlus} size={1} />}
          onClick={() => {
            setActiveNotice(null);
            setIsEditModalOpen(true);
          }}
        >
          新建通知
        </Button>
      </Center>

      <NoticeEditModal
        centered
        size="30%"
        title={activeNotice ? '编辑通知' : '新建通知'}
        notice={activeNotice}
        opened={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        mutateNotice={(notice: Notice) => {
          mutate([notice, ...(notices?.filter((n) => n.id !== notice.id) ?? [])]);
        }}
      />

      <Modal
        opened={isDeleteModalOpen}
        onClose={() => setIsDeleteModalOpen(false)}
        centered
        title="删除通知"
        withCloseButton={false}
      >
        <Stack>
          <Text size="sm">你确定要删除 "{deleteNotice?.title ?? ''}" 吗？</Text>
          <Group grow style={{ margin: 'auto', width: '100%' }}>
            <Button fullWidth color="red" variant="outline" onClick={onConfirmDelete}>
              确认删除
            </Button>
            <Button fullWidth onClick={() => setIsDeleteModalOpen(false)}>
              取消
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
};

export default NoticeManager;

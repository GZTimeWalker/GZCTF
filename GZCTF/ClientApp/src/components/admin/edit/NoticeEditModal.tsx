import { FC, useEffect } from 'react';
import { mutate } from 'swr';
import { Button, Group, Modal, ModalProps, Stack, Text, Textarea, TextInput } from '@mantine/core';
import { useInputState } from '@mantine/hooks';
import { showNotification } from '@mantine/notifications';
import { mdiCloseCircle, mdiCheck, mdiClose } from '@mdi/js';
import Icon from '@mdi/react';
import api, { Notice } from '../../../Api';

interface NoticeEditModalProps extends ModalProps {
  notice?: Notice | null;
  mutateNotice: (notice: Notice) => void;
}

const NoticeEditModal: FC<NoticeEditModalProps> = (props) => {
  const { notice, mutateNotice, ...modalProps } = props;

  const [title, setTitle] = useInputState(notice?.title);
  const [content, setContent] = useInputState(notice?.content);

  useEffect(() => {
    setTitle(notice?.title);
    setContent(notice?.content);
  }, [notice]);

  const onUpdate = () => {
    if (!title || !content) {
      showNotification({
        color: 'red',
        title: '输入不能为空',
        message: '请输入标题和内容',
        icon: <Icon path={mdiClose} size={1} />,
      });
      return;
    }

    if (notice) {
      api.edit
        .editUpdateNotice(notice.id!, {
          title: title,
          content: content,
        })
        .then((data) => {
          showNotification({
            color: 'teal',
            message: '通知修改成功',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          });
          mutateNotice(data.data);
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '遇到了问题',
            message: `${err.error.title}`,
            icon: <Icon path={mdiClose} size={1} />,
          });
        });
    } else {
      api.edit
        .editAddNotice({
          title: title,
          content: content,
        })
        .then((data) => {
          showNotification({
            color: 'teal',
            message: '通知已添加',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          });
          mutateNotice(data.data);
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '遇到了问题',
            message: `${err.error.title}`,
            icon: <Icon path={mdiClose} size={1} />,
          });
        });
    }
  };

  const onClear = () => {
    setTitle(notice?.title);
    setContent(notice?.content);
  };

  return (
    <Modal {...modalProps}>
      <Stack>
        <TextInput
          label="通知标题"
          type="text"
          placeholder="Title"
          style={{ width: '100%' }}
          value={title}
          onChange={setTitle}
        />
        <Textarea
          label={
            <Group spacing="sm">
              <Text>通知详情</Text>
              <Text size="xs" color="gray">
                支持 markdown 语法
              </Text>
            </Group>
          }
          value={content}
          style={{ width: '100%' }}
          autosize
          minRows={5}
          maxRows={16}
          onChange={setContent}
        />
        <Group grow style={{ margin: 'auto', width: '100%' }}>
          <Button fullWidth variant="outline" onClick={onClear}>
            {notice ? '还原通知' : '清空通知'}
          </Button>
          <Button fullWidth variant="outline" color="orange" onClick={onUpdate}>
            {notice ? '更改通知' : '新建通知'}
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
};

export default NoticeEditModal;

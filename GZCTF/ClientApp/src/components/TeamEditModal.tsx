import { FC, useEffect, useState } from 'react';
import {
  Avatar,
  Box,
  Button,
  Center,
  Grid,
  Group,
  Title,
  Image,
  Modal,
  ModalProps,
  Stack,
  Text,
  Textarea,
  TextInput,
  useMantineTheme,
  PasswordInput,
} from '@mantine/core';
import { Dropzone, DropzoneStatus, IMAGE_MIME_TYPE } from '@mantine/dropzone';
import { useClipboard } from '@mantine/hooks';
import { showNotification } from '@mantine/notifications';
import { mdiCheck, mdiClose, mdiCloseCircle } from '@mdi/js';
import Icon from '@mdi/react';
import api, { TeamInfoModel } from '../Api';

const dropzoneChildren = (status: DropzoneStatus, file: File | null) => (
  <Group position="center" spacing="xl" style={{ minHeight: 240, pointerEvents: 'none' }}>
    {file ? (
      <Image fit="contain" src={URL.createObjectURL(file)} alt="avatar" />
    ) : (
      <Box>
        <Text size="xl" inline>
          拖放图片或点击此处以选择头像
        </Text>
        <Text size="sm" color="dimmed" inline mt={7}>
          请选择小于 3MB 的图片
        </Text>
      </Box>
    )}
  </Group>
);

interface TeamEditModalProps extends ModalProps {
  team: TeamInfoModel | null;
  isCaptain: boolean;
}

const TeamEditModal: FC<TeamEditModalProps> = (props) => {
  const { team, isCaptain, ...modalProps } = props;

  const [teamInfo, setTeamInfo] = useState<TeamInfoModel | null>(team);
  const [dropzoneOpened, setDropzoneOpened] = useState(false);
  const [avatarFile, setAvatarFile] = useState<File | null>(null);

  const [leaveOpened, setLeaveOpened] = useState(false);

  const theme = useMantineTheme();
  const clipboard = useClipboard({ timeout: 500 });

  const [inviteCode, setInviteCode] = useState('');

  useEffect(() => {
    setTeamInfo(team);
    if (isCaptain && !inviteCode) {
      api.team.teamTeamInviteCode(team?.id!).then((code) => {
        setInviteCode(code.data);
      });
    }
  }, [team, inviteCode, isCaptain]);

  const onConfirmLeaveTeam = () => {
    if (teamInfo && !isCaptain) {
      api.team
        .teamLeave(teamInfo.id!)
        .then(() => {
          showNotification({
            color: 'teal',
            title: '退出队伍成功',
            message: '您的队伍信息已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          });
          api.team.mutateTeamGetTeamsInfo();
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
          setLeaveOpened(false);
        });
    }
  };

  const onChangeAvatar = () => {
    if (avatarFile && teamInfo?.id) {
      api.team
        .teamAvatar(teamInfo?.id, {
          file: avatarFile,
        })
        .then((data) => {
          showNotification({
            color: 'teal',
            title: '修改头像成功',
            message: '您的头像已经更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          });
          setTeamInfo({ ...teamInfo, avatar: data.data });
          api.team.mutateTeamGetTeamsInfo();
          setAvatarFile(null);
          setDropzoneOpened(false);
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '遇到了问题',
            message: `${err.error.title}`,
            icon: <Icon path={mdiClose} size={1} />,
          });
          setDropzoneOpened(false);
        });
    }
  };

  const onSaveChange = () => {
    if (teamInfo && teamInfo?.id) {
      api.team
        .teamUpdateTeam(teamInfo.id, teamInfo)
        .then((data) => {
          // Updated TeamInfoModel
          showNotification({
            color: 'teal',
            title: '你改变了一些东西',
            message: '但值得吗？',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          });
          api.team.mutateTeamGetTeamsInfo();
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

  return (
    <Modal {...modalProps}>
      <Stack spacing="lg">
        {/* Team Info */}
        <Grid grow>
          <Grid.Col span={8}>
            <TextInput
              label="队伍名称"
              type="text"
              placeholder={team?.name ?? 'ctfteam'}
              style={{ width: '100%' }}
              value={teamInfo?.name ?? 'team'}
              disabled={!isCaptain}
              onChange={(event) => setTeamInfo({ ...teamInfo, name: event.target.value })}
            />
          </Grid.Col>
          <Grid.Col span={4}>
            <Center>
              <Avatar
                style={{ borderRadius: '50%' }}
                size={70}
                src={teamInfo?.avatar}
                onClick={() => isCaptain && setDropzoneOpened(true)}
              />
            </Center>
          </Grid.Col>
        </Grid>
        {isCaptain && (
          <PasswordInput
            label="邀请码"
            value={inviteCode}
            placeholder="loading..."
            onClick={() => {
              clipboard.copy(inviteCode);
              showNotification({
                color: 'teal',
                message: '邀请码已复制',
                icon: <Icon path={mdiCheck} size={1} />,
                disallowClose: true,
              });
            }}
            readOnly
          />
        )}

        <Textarea
          label="队伍签名"
          placeholder={teamInfo?.bio ?? '这个人很懒，什么都没有写'}
          value={teamInfo?.bio ?? '这个人很懒，什么都没有写'}
          style={{ width: '100%' }}
          disabled={!isCaptain}
          autosize
          minRows={2}
          maxRows={4}
          onChange={(event) => setTeamInfo({ ...teamInfo, bio: event.target.value })}
        />
        <Group grow style={{ margin: 'auto', width: '100%' }}>
          <Button fullWidth color="red" variant="outline" onClick={() => setLeaveOpened(true)}>
            {isCaptain ? '删除队伍' : '退出队伍'}
          </Button>
          <Button fullWidth disabled={!isCaptain} onClick={onSaveChange}>
            更新队伍
          </Button>
        </Group>
      </Stack>

      {/* 更新头像浮窗 */}
      <Modal
        opened={dropzoneOpened}
        onClose={() => setDropzoneOpened(false)}
        centered
        withCloseButton={false}
      >
        <Dropzone
          onDrop={(files) => setAvatarFile(files[0])}
          onReject={() => {
            showNotification({
              color: 'red',
              title: '文件上传失败',
              message: `请重新提交`,
              icon: <Icon path={mdiClose} size={1} />,
            });
          }}
          style={{
            margin: '0 auto 20px auto',
            minWidth: '220px',
            minHeight: '220px',
          }}
          maxSize={3 * 1024 * 1024}
          accept={IMAGE_MIME_TYPE}
        >
          {(status) => dropzoneChildren(status, avatarFile)}
        </Dropzone>
        <Button fullWidth variant="outline" onClick={onChangeAvatar}>
          更新头像
        </Button>
      </Modal>

      {/* 删除队伍浮窗 */}
      <Modal
        opened={leaveOpened}
        centered
        title={isCaptain ? '删除队伍' : '离开队伍'}
        onClose={() => setLeaveOpened(false)}
      >
        {isCaptain ? (
          <Stack spacing="lg" p={40} style={{ textAlign: 'center' }}>
            <Center>
              <Icon color={theme.colors.red[7]} path={mdiCloseCircle} size={4} />
            </Center>
            <Title order={3}>暂不允许删除战队</Title>
            <Text>
              为保证数据完整性
              <br />
              删除战队功能已被禁用
            </Text>
          </Stack>
        ) : (
          <Stack>
            <Text size="sm">你确定要离开 {teamInfo?.name} 吗？</Text>
            <Group grow style={{ margin: 'auto', width: '100%' }}>
              <Button fullWidth color="red" variant="outline" onClick={onConfirmLeaveTeam}>
                确认离开
              </Button>
              <Button fullWidth onClick={() => setLeaveOpened(false)}>
                取消
              </Button>
            </Group>
          </Stack>
        )}
      </Modal>
    </Modal>
  );
};

export default TeamEditModal;

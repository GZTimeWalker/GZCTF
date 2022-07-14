import type { NextPage } from 'next';
import { useEffect, useState } from 'react';
import {
  Box,
  Stack,
  Group,
  Divider,
  Text,
  Button,
  Grid,
  TextInput,
  Paper,
  Textarea,
  Modal,
  Avatar,
  Image,
  Center,
  SimpleGrid,
} from '@mantine/core';
import { Dropzone, DropzoneStatus, IMAGE_MIME_TYPE } from '@mantine/dropzone';
import { showNotification } from '@mantine/notifications';
import { mdiCheck, mdiClose } from '@mdi/js';
import Icon from '@mdi/react';
import api, { ProfileUpdateModel } from '../../Api';
import PasswordChangeModal from '../../components/PasswordChangeModal';
import WithNavBar from '../../components/WithNavbar';

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

const Profile: NextPage = () => {
  const [dropzoneOpened, setDropzoneOpened] = useState(false);
  const { data, mutate } = api.account.useAccountProfile({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  });

  const [profile, setProfile] = useState<ProfileUpdateModel>({
    userName: data?.userName,
    bio: data?.bio,
    stdNumber: data?.stdNumber,
    phone: data?.phone,
    realName: data?.realName,
  });
  const [avatarFile, setAvatarFile] = useState<File | null>(null);

  const [disabled, setDisabled] = useState(false);

  const [mailEditOpened, setMailEditOpened] = useState(false);
  const [pwdChangeOpened, setPwdChangeOpened] = useState(false);

  const [email, setEmail] = useState(data?.email ?? '');

  useEffect(() => {
    setProfile({
      userName: data?.userName,
      bio: data?.bio,
      stdNumber: data?.stdNumber,
      phone: data?.phone,
      realName: data?.realName,
    });
  }, [data]);

  const onChangeAvatar = () => {
    if (avatarFile) {
      api.account
        .accountAvatar({
          file: avatarFile,
        })
        .then(() => {
          showNotification({
            color: 'teal',
            title: '修改头像成功',
            message: '您的头像已经更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          });
          mutate({ ...data });
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

  const onChangeProfile = () => {
    api.account
      .accountUpdate(profile)
      .then(() => {
        showNotification({
          color: 'teal',
          title: '更改成功',
          message: '个人信息已更新',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        });
        mutate({ ...data });
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        });
      });
  };

  const onChangeEmail = () => {
    if (email) {
      api.account
        .accountChangeEmail({
          newMail: email,
        })
        .then(() => {
          showNotification({
            color: 'teal',
            title: '验证邮件已发送',
            message: '请检查你的邮箱及垃圾邮件~',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          });
          setMailEditOpened(false);
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
    <WithNavBar>
      <Center style={{ height: '90vh' }}>
        <Paper style={{ width: '55%', padding: '2% 5%' }} shadow="sm" p="lg">
          {/* Header */}
          <Box style={{ marginBottom: '5px' }}>
            <h2>个人信息</h2>
          </Box>
          <Divider />

          {/* User Info */}
          <Stack spacing="md" style={{ margin: 'auto', marginTop: '15px' }}>
            <Grid grow>
              <Grid.Col span={8}>
                <TextInput
                  label="用户名"
                  type="text"
                  style={{ width: '100%' }}
                  value={profile.userName ?? 'ctfer'}
                  disabled={disabled}
                  onChange={(event) => setProfile({ ...profile, userName: event.target.value })}
                />
              </Grid.Col>
              <Grid.Col span={4}>
                <Center>
                  <Avatar
                    style={{ borderRadius: '50%' }}
                    size={70}
                    src={data?.avatar}
                    onClick={() => setDropzoneOpened(true)}
                  />
                </Center>
              </Grid.Col>
            </Grid>
            <TextInput
              label="邮箱"
              type="email"
              style={{ width: '100%' }}
              value={data?.email ?? 'ctfer@gzti.me'}
              disabled={disabled}
              readOnly
            />
            <TextInput
              label="手机号"
              type="tel"
              style={{ width: '100%' }}
              value={profile.phone ?? ''}
              disabled={disabled}
              onChange={(event) => setProfile({ ...profile, phone: event.target.value })}
            />
            <SimpleGrid cols={2}>
              <TextInput
                label="学工号"
                type="number"
                style={{ width: '100%' }}
                value={profile.stdNumber ?? ''}
                disabled={disabled}
                onChange={(event) => setProfile({ ...profile, stdNumber: event.target.value })}
              />
              <TextInput
                label="真实姓名"
                type="text"
                style={{ width: '100%' }}
                value={profile.realName ?? ''}
                disabled={disabled}
                onChange={(event) => setProfile({ ...profile, realName: event.target.value })}
              />
            </SimpleGrid>
            <Textarea
              label="描述"
              value={profile.bio ?? '这个人很懒，什么都没有写'}
              style={{ width: '100%' }}
              disabled={disabled}
              autosize
              minRows={2}
              maxRows={4}
              onChange={(event) => setProfile({ ...profile, bio: event.target.value })}
            />
            <Box style={{ margin: 'auto', width: '100%' }}>
              <Grid grow>
                <Grid.Col span={4}>
                  <Button
                    fullWidth
                    color="red"
                    variant="outline"
                    disabled={disabled}
                    onClick={() => setMailEditOpened(true)}
                  >
                    更改邮箱
                  </Button>
                </Grid.Col>
                <Grid.Col span={4}>
                  <Button
                    fullWidth
                    color="red"
                    variant="outline"
                    disabled={disabled}
                    onClick={() => setPwdChangeOpened(true)}
                  >
                    更改密码
                  </Button>
                </Grid.Col>
                <Grid.Col span={4}>
                  <Button fullWidth disabled={disabled} onClick={onChangeProfile}>
                    保存信息
                  </Button>
                </Grid.Col>
              </Grid>
            </Box>
          </Stack>

          {/* Change Password */}
          <PasswordChangeModal
            opened={pwdChangeOpened}
            centered
            onClose={() => setPwdChangeOpened(false)}
            title="更改密码"
          />

          {/* Change Email */}
          <Modal
            opened={mailEditOpened}
            centered
            onClose={() => setMailEditOpened(false)}
            title="更改邮箱"
          >
            <Stack>
              <Text>
                更改邮箱后，您将不能通过原邮箱登录。一封邮件将会发送至您的新邮箱，请点击邮件中的链接完成验证。
              </Text>
              <TextInput
                required
                label="新邮箱"
                type="email"
                style={{ width: '100%' }}
                placeholder={data?.email ?? 'ctfer@gzti.me'}
                value={email}
                onChange={(event) => setEmail(event.target.value)}
              />
              <Grid grow>
                <Grid.Col span={6}>
                  <Button fullWidth color="red" variant="outline" onClick={onChangeEmail}>
                    确认修改
                  </Button>
                </Grid.Col>
                <Grid.Col span={6}>
                  <Button
                    fullWidth
                    variant="outline"
                    onClick={() => {
                      setEmail(data?.email ?? '');
                      setMailEditOpened(false);
                    }}
                  >
                    取消修改
                  </Button>
                </Grid.Col>
              </Grid>
            </Stack>
          </Modal>

          {/* Change avatar */}
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
            <Button fullWidth variant="outline" disabled={disabled} onClick={onChangeAvatar}>
              修改头像
            </Button>
          </Modal>
        </Paper>
      </Center>
    </WithNavBar>
  );
};

export default Profile;

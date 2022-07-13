import type { NextPage } from 'next';
import { useState } from 'react';
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
  PasswordInput,
  Avatar,
  MantineTheme,
  useMantineTheme,
} from '@mantine/core';
import { Dropzone, DropzoneStatus, IMAGE_MIME_TYPE } from '@mantine/dropzone';
import { useInputState } from '@mantine/hooks';
import { showNotification } from '@mantine/notifications';
import { mdiAirHorn, mdiCheck, mdiClose } from '@mdi/js';
import Icon from '@mdi/react';
import api from '../../Api';
import WithNavBar from '../../components/WithNavbar';

const dropzoneChildren = (status: DropzoneStatus, theme: MantineTheme, file: File) => (
  <Group position="center" spacing="xl" style={{ minHeight: 220, pointerEvents: 'none' }}>
    <div>
      <Text size="xl" inline>
        拖放图片或点击此处以选择头像
      </Text>
      <Text size="sm" color="dimmed" inline mt={7}>
        请选择小于 5MB 的图片
      </Text>
    </div>
    <Text size='md'>{file.name}</Text>
  </Group>
);


const Profile: NextPage = () => {
  const theme = useMantineTheme()
  const [opened, setOpened] = useState(false);
  const [dropzoneOpened, setDropzoneOpened] = useState(false);
  const { data, mutate } = api.account.useAccountProfile({
    refreshInterval: 600,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  });
  const [disabled, setDisabled] = useState(false);
  const [pwdDisabled, setPwdDisabled] = useState(false);
  const [uname, setUname] = useInputState('');
  const [bio, setBio] = useInputState('');
  const [pwd, setPwd] = useInputState('');
  const [email, setEmail] = useInputState('');
  const [avatarFile, setAvatarFile] = useState<File | null>(null);

  const onSaveChange = () => {
    setDisabled(true);
    if (email && email != data?.email) {
      setOpened(true);
    } else if ((uname && uname != data?.userName) || (bio && bio != data?.bio)) {
      changeInfo();
    } else {
      showNotification({
        color: 'red',
        title: '你没有做任何修改',
        message: '这很值得',
        icon: <Icon path={mdiAirHorn} size={1} />,
        disallowClose: true,
      });
    }
    setDisabled(false);
  };

  const onConfirmEmail = () => {
    setPwdDisabled(true);
    // Need a check code API
    setOpened(false);
    changeEmailAndInfo();
    setPwdDisabled(false);
  };

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

  const onClearInfo = () => {
    setDisabled(true);

    setUname('');
    setBio('');
    setEmail('');

    setDisabled(false);
  };

  const changeInfo = () => {
    if (uname || bio) {
      api.account
        .accountUpdate({
          userName: uname || data?.userName,
          bio: bio || data?.bio,
        })
        .then(() => {
          showNotification({
            color: 'teal',
            title: '你改变了一些东西',
            message: '但值得吗？',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          });
          onClearInfo();
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
    }
  };

  const changeEmailAndInfo = () => {
    if (email) {
      api.account
        .accountChangeEmail({
          newMail: email,
        })
        .then(() => {
          showNotification({
            color: 'teal',
            title: '确认邮件已发送',
            message: '请在您的邮箱中查收',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          });
          changeInfo();
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
    <>
      {/* Main Page */}
      <WithNavBar>
        <Stack align="center">
          <Paper
            style={{ width: '55%', padding: '5%', paddingTop: '2%', marginTop: '5%' }}
            shadow="sm"
            p="lg"
          >
            {/* Header */}
            <Box style={{ marginBottom: '5px' }}>
              <h2>个人信息</h2>
            </Box>
            <Divider />

            <Grid style={{ marginTop: '15px' }}>
              {/* User Info */}
              <Grid.Col span={9}>

                {/* OverLay */}
                <Modal opened={opened} onClose={() => setOpened(false)} title="您即将修改邮箱，请确认密码">
                  <PasswordInput
                    required
                    label="密码"
                    placeholder="P4ssW@rd"
                    style={{ width: '100%' }}
                    value={pwd}
                    disabled={pwdDisabled}
                    onChange={(event) => setPwd(event.currentTarget.value)}
                  />
                  <Box style={{ margin: 'auto', marginTop: '30px' }}>
                    {
                      <Grid grow>
                        <Grid.Col span={8}>
                          <Button fullWidth variant="outline" onClick={onConfirmEmail}>
                            确认修改
                          </Button>
                        </Grid.Col>
                        <Grid.Col span={4}>
                          <Button fullWidth color="red" variant="outline" onClick={() => setOpened(false)}>
                            取消修改
                          </Button>
                        </Grid.Col>
                      </Grid>
                    }
                  </Box>
                </Modal>

                {/* Input and Info display */}
                <Group position="apart" style={{ margin: 'auto' }}>
                  <TextInput
                    label="用户名"
                    type="text"
                    placeholder={data?.userName ?? 'ctfer'}
                    style={{ width: '200px' }}
                    value={uname}
                    disabled={disabled}
                    onChange={(event) => setUname(event.currentTarget.value)}
                  />
                  <TextInput
                    label="邮箱"
                    type="email"
                    placeholder={data?.email ?? 'ctfer@GZ.cn'}
                    style={{ width: '100%' }}
                    value={email}
                    disabled={disabled}
                    onChange={(event) => setEmail(event.currentTarget.value)}
                  />
                  <Textarea
                    label="描述"
                    placeholder={data?.bio ?? '这个人很懒，什么都没有写'}
                    value={bio}
                    style={{ width: '100%' }}
                    disabled={disabled}
                    autosize
                    minRows={2}
                    maxRows={4}
                    onChange={(event) => setBio(event.currentTarget.value)}
                  />
                </Group>

                {/* Button: save Changes or clear Changes */}
                <Box style={{ margin: 'auto', marginTop: '30px' }}>
                  {
                    <Grid grow>
                      <Grid.Col span={8}>
                        <Button
                          fullWidth
                          variant="outline"
                          disabled={disabled}
                          onClick={onSaveChange}
                        >
                          保存变更
                        </Button>
                      </Grid.Col>
                      <Grid.Col span={4}>
                        <Button
                          fullWidth
                          color="red"
                          variant="outline"
                          disabled={disabled}
                          onClick={onClearInfo}
                        >
                          清除变更
                        </Button>
                      </Grid.Col>
                    </Grid>
                  }
                </Box>
              </Grid.Col>

              {/* User Avator */}
              <Grid.Col span={3}>
                <Stack align="center" style={{ width: '100%', marginTop: '10px' }}>
                  {/* Dropzone */}
                  <Modal opened={dropzoneOpened} onClose={() => setDropzoneOpened(false)} withCloseButton={false}>
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
                      maxSize={3 * 1024 ** 2}
                      accept={IMAGE_MIME_TYPE}
                    >
                      {(status) => dropzoneChildren(status, theme, avatarFile ?? new File([], ''))}
                    </Dropzone>
                    <Button
                      fullWidth
                      variant="outline"
                      disabled={disabled}
                      onClick={onChangeAvatar}
                    >
                      修改头像
                    </Button>
                  </Modal>

                  {/* Avatar */}
                  <Avatar
                    style={{ borderRadius: '50%' }}
                    size="xl"
                    src={data?.avatar}
                    onClick={() => setDropzoneOpened(true)}
                  />
                </Stack>
              </Grid.Col>
            </Grid>
          </Paper>
        </Stack>
      </WithNavBar>
    </>
  );
};

export default Profile;

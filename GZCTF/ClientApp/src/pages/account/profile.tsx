import type { NextPage } from 'next';
import { useState } from 'react';
import {
  Box,
  Stack,
  Group,
  Divider,
  Title,
  Text,
  Button,
  Input,
  Grid,
  Card,
  TextInput,
  Paper,
  Center,
  Textarea,
  Modal,
  PasswordInput,
  Avatar,
  Image,
  Indicator,
  Header,
} from '@mantine/core';
import { Dropzone, DropzoneStatus, IMAGE_MIME_TYPE } from '@mantine/dropzone';
import { useInputState } from '@mantine/hooks';
import { showNotification } from '@mantine/notifications';
import { mdiAccessPoint, mdiAirHorn, mdiCheck, mdiClose } from '@mdi/js';
import Icon from '@mdi/react';
import api from '../../Api';
import WithNavBar from '../../components/WithNavbar';

const Profile: NextPage = () => {
  const [opened, setOpened] = useState(false);
  const { data, mutate } = api.account.useAccountProfile();
  const [disabled, setDisabled] = useState(false);
  const [pwdDisabled, setPwdDisabled] = useState(false);
  const [uname, setUname] = useInputState('');
  const [bio, setBio] = useInputState('');
  const [pwd, setPwd] = useInputState('');
  const [avatar, setAvatar] = useState(data?.avatar || '');
  const [email, setEmail] = useInputState('');

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
    /*
    TODO: Profile props, update with mutate, avatar with DropZone
    Refer: https://mantine.dev/others/dropzone/
  */
    showNotification({
      color: 'teal',
      title: '未实现',
      message: '但你为什么要换头像捏',
      icon: <Icon path={mdiAccessPoint} size={1} />,
      disallowClose: true,
    });
  };

  const onClearInfo = () => {
    setDisabled(true);

    setUname('');
    setBio('');
    setEmail('');
    // setAvatar(data?.avatar);

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
              <Header height={36}>个人信息</Header>
            </Box>
            <Divider />
            <Grid style={{ marginTop: '15px' }}>
              {/* User Info */}
              <Grid.Col span={9}>
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
                <Text size="sm">用户头像</Text>
                <Stack align="center" style={{ width: '100%' }}>
                  <Avatar
                    style={{ borderRadius: '50%' }}
                    size="xl"
                    src={avatar ?? 'https://mantine.dev/images/avatar.png'}
                    onClick={onChangeAvatar}
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

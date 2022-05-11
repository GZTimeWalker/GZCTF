import type { NextPage } from 'next';
import { Box, Stack, Group, Divider, Title, Text, Button, Input, Grid, Card, TextInput, Paper, Center } from '@mantine/core';
import { Dropzone, DropzoneStatus, IMAGE_MIME_TYPE } from '@mantine/dropzone';
import api from '../../Api';
import WithNavBar from '../../components/WithNavbar';
import { useState } from 'react';
import { useInputState } from '@mantine/hooks';
import { showNotification } from '@mantine/notifications';
import Icon from '@mdi/react';
import { mdiCheck, mdiClose } from '@mdi/js';

const Profile: NextPage = () => {
  const { data, mutate } = api.account.useAccountProfile();
  const [disabled, setDisabled] = useState(false);
  const [uname, setUname] = useInputState(data?.userName||'');
  const [bio, setBio] = useInputState(data?.bio||'');

  // use to update: mutate({userName: 'Hello', ...data})
  const onChangeInfo = () => {
    setDisabled(true);

    api.account
    .accountUpdate({
      userName: uname||data?.userName,
      bio: bio||data?.bio,
      })
      .then(() => {
        showNotification({
          color: 'teal',
          title: '你改变了一些东西',
          message: '但值得吗？',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        });
        mutate({...data})
        setDisabled(false)
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        });
        setDisabled(false);
      });
  }

  return (
    <WithNavBar>
      <Stack align="center">
        <Divider style = {{ width: "60%" }} />

        <Paper shadow = "sm" p="lg" style = {{ width: "60%" }}>
          <Group position='apart' style={{ margin: "auto", width: "100%" }}>
            <TextInput
              label="用户名"
              type="text"
              placeholder={data?.userName ?? "ctfer"}
              style={{ width: '100%' }}
              value={uname}
              disabled={disabled}
              onChange={(event) => setUname(event.currentTarget.value)}
            />
            <TextInput
              label="描述"
              type="text"
              placeholder={data?.bio ?? "这个人很懒，什么都没有写"}
              style={{ width: '100%' }}
              value={bio}
              disabled={disabled}
              onChange={(event) => setBio(event.currentTarget.value)}
            />
          </Group>
          <Box style={{ margin: "auto", marginTop: "30px", width: '100%' }}>
            {/*
              TODO: Profile props, update with mutate, avatar with DropZone
              Refer: https://mantine.dev/others/dropzone/
            */
              <Button fullWidth variant="outline" disabled={disabled} onClick={onChangeInfo}>
                保存变更
              </Button>
            }
          </Box>
        </Paper>
      </Stack>
    </WithNavBar>
  );
};

export default Profile;

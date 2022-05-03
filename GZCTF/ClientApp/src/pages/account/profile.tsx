import type { NextPage } from 'next';
import { Box, Stack, Group, Divider, Title, Text } from '@mantine/core';
import { Dropzone, DropzoneStatus, IMAGE_MIME_TYPE } from '@mantine/dropzone';
import api from '../../Api';
import WithNavBar from '../../components/WithNavbar';

const Profile: NextPage = () => {
  const { data } = api.account.useAccountProfile();
  const mutate = api.account.mutateAccountProfile;

  // use to update: mutate({userName: 'Hello', ...data})

  return (
    <WithNavBar>
      <Stack>
        <Title>{data?.userName}</Title>
        <Divider />
        <Group>
          <Box style={{ width: '30%' }}>
            {/*
              TODO: Profile props, update with mutate, avatar with DropZone
              Refer: https://mantine.dev/others/dropzone/
            */}
          </Box>
        </Group>
      </Stack>
    </WithNavBar>
  );
};

export default Profile;

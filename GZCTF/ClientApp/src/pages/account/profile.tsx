import type { NextPage } from 'next';
import { Text, Stack, Group, Title, Center } from '@mantine/core';
import WithNavBar from '../../components/WithNavbar';
import { useState } from 'react';
import useSWR from 'swr';
import { ClientUserInfoModel } from '../../client';
import { useSWRHandler } from 'swr/dist/use-swr';

const Profile: NextPage = () => {
    const { data } = useSWR<ClientUserInfoModel>('/api/account/profile');

  return (
    <WithNavBar>
        <Stack>

        </Stack>
    </WithNavBar>
  );
};

export default Profile;

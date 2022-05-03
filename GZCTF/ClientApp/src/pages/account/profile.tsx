import type { NextPage } from 'next';
import { useState } from 'react';
import useSWR from 'swr';
import { Text, Stack, Group, Title, Center } from '@mantine/core';
import api from '../../Api';
import WithNavBar from '../../components/WithNavbar';

const Profile: NextPage = () => {
  return (
    <WithNavBar>
      <Stack></Stack>
    </WithNavBar>
  );
};

export default Profile;

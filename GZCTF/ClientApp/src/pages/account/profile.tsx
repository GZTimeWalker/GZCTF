import type { NextPage } from 'next';
import { Text, Stack, Group, Title, Center } from '@mantine/core';
import WithNavBar from '../../components/WithNavbar';
import { useState } from 'react';
import useSWR from 'swr';
import api from '../../Api';

const Profile: NextPage = () => {
  return (
    <WithNavBar>
      <Stack></Stack>
    </WithNavBar>
  );
};

export default Profile;

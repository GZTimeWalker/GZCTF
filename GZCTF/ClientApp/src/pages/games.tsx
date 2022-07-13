import type { NextPage } from 'next';
import { Text, Stack, Center } from '@mantine/core';
import LogoHeader from '../components/LogoHeader';
import WithNavBar from '../components/WithNavbar';

const Games: NextPage = () => {
  return (
    <WithNavBar>
      <Stack>
        <LogoHeader />
      </Stack>
    </WithNavBar>
  );
};

export default Games;

import type { NextPage } from 'next';
import { Stack } from '@mantine/core';
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

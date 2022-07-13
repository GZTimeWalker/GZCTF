import type { NextPage } from 'next';
import { Stack, Card, Group, Badge, Text, useMantineTheme } from '@mantine/core';
import api from '../Api';
import LogoHeader from '../components/LogoHeader';
import NoticeCard from '../components/NoticeCard';
import WithNavBar from '../components/WithNavbar';

const Home: NextPage = () => {
  const { data } = api.info.useInfoGetNotices({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  });

  return (
    <WithNavBar>
      <Stack align="center">
        <LogoHeader />
        {data?.map((notice) => (
          <NoticeCard key={notice.id} {...notice} />
        ))}
      </Stack>
    </WithNavBar>
  );
};

export default Home;

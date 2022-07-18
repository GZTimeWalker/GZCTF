import { FC } from 'react';
import { Stack } from '@mantine/core';
import api from '../Api';
import LogoHeader from '../components/LogoHeader';
import NoticeCard from '../components/NoticeCard';
import WithNavBar from '../components/WithNavbar';

const Home: FC = () => {
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

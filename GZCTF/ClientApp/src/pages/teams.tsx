import type { NextPage } from 'next';
import { Stack, SimpleGrid, Loader, Center } from '@mantine/core';
import api from '../Api';
import LogoHeader from '../components/LogoHeader';
import TeamCard from '../components/TeamCard';
import WithNavBar from '../components/WithNavbar';

const Teams: NextPage = () => {
  const {
    data: teams,
    error
  } = api.team.useTeamGetTeamsInfo({
    refreshInterval: 3000,
  });

  return (
    <WithNavBar>
      <Stack>
        <LogoHeader />
        {teams && !error ? (
          <SimpleGrid cols={3} spacing="lg">
            {teams.map((t, i) => <TeamCard key={i} {...t} />)}
          </SimpleGrid>
        ) : (
          <Center style={{ width: '100%', height: '100%' }}>
            <Loader />
          </Center>
        )}
      </Stack>
    </WithNavBar>
  );
};

export default Teams;

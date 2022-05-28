import type { NextPage } from 'next';
import LogoHeader from '../components/LogoHeader';
import WithNavBar from '../components/WithNavbar';
import api from '../Api';
import { Stack, SimpleGrid } from '@mantine/core';
import TeamCard from '../components/TeamCard';

const Teams: NextPage = () => {
  const { data: teams, error } = api.team.useTeamGetTeamsInfo({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  });

  return (
    <WithNavBar>
      <Stack>
        <LogoHeader />
        <SimpleGrid cols={3} spacing="lg">
          {teams && !error &&
            teams.map((t, i) => <TeamCard key={i} {...t} />)
          }
        </SimpleGrid>
      </Stack>
    </WithNavBar>
  );
};

export default Teams;

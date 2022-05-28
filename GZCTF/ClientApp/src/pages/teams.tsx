import type { NextPage } from 'next';
import { Text, Stack, Center, SimpleGrid, Card, Grid, Avatar, Title, Container, Group, Divider, AvatarsGroup, Badge, useMantineTheme } from '@mantine/core';
import LogoHeader from '../components/LogoHeader';
import WithNavBar from '../components/WithNavbar';
import Icon from "@mdi/react";
import { mdiLock, mdiLockOutline } from "@mdi/js";

const Teams: NextPage = () => {
  const theme = useMantineTheme();
  interface Team {
    name: string,
    descrp: string,
    isActive: boolean,
    status: number,
    region: string,
    members: number[],
    isLocked: boolean,
  };

  const fake_teams: Team[] = [
    {
      name: "Team1",
      descrp: "desc1",
      isActive: true,
      status: 1,
      region: "China",
      members: [0, 1, 2],
      isLocked: true,
    },
    {
      name: "Team2",
      descrp: "desc2",
      isActive: true,
      status: 0,
      region: "China",
      members: [2, 4],
      isLocked: true,
    },
    {
      name: "Team3",
      descrp: "desc3",
      isActive: false,
      status: 1,
      region: "USA",
      members: [8],
      isLocked: false,
    },
    {
      name: "Team4",
      descrp: "desc4",
      isActive: true,
      status: 1,
      region: "China",
      members: [3, 2, 5, 7],
      isLocked: false,
    },
    {
      name: "Team5",
      descrp: "desc5",
      isActive: false,
      status: 1,
      region: "Japan",
      members: [4, 3, 2, 5, 7, 1, 22, 9, 8],
      isLocked: true,
    },
    {
      name: "Team6",
      descrp: "desc6",
      isActive: false,
      status: 0,
      region: "China",
      members: [6],
      isLocked: false,
    },
  ];

  return (
    <WithNavBar>
      <Stack>
        <LogoHeader />
        <SimpleGrid cols={3} spacing="lg">
          {fake_teams.map((t, i) =>
            <Card shadow="sm">
              <Group align="stretch">
                <Avatar color="cyan" size="lg" radius="md">{t.name[0]}</Avatar>
                <div style={{ flexGrow: 1 }}>
                  <Title order={2} align="left">{t.name}</Title>
                  <Text size="md">{t.descrp}</Text>
                </div>
                <div style={{ height: "100%" }}>
                  {i == 0
                    ? <Text transform="uppercase" size="xs" color="yellow">Active</Text>
                    : <Text transform="uppercase" size="xs">Inactive</Text>}
                </div>
              </Group>
              <Divider my="sm" />
              <Stack spacing="xs">
                <Group spacing="xs" position="apart">
                  <Text transform="uppercase" color="dimmed">Role: </Text>
                  {t.status
                    ? <Badge color="gray" size="lg">crew</Badge>
                    : <Badge color="brand" size="lg">captain</Badge>}
                </Group>
                <Group spacing="xs">
                  <Text transform="uppercase" color="dimmed">MEMBERS: </Text>
                  <div style={{ flexGrow: 1 }}></div>
                  {t.isLocked && <Icon path={mdiLockOutline} size={1} color={theme.colors.alert[1]} />}
                  <AvatarsGroup limit={3}>
                    {t.members.map(m =>
                      <Avatar radius="xl" />)}
                  </AvatarsGroup>
                </Group>
                <Group spacing="xs" position="apart">
                  <Text transform="uppercase" color="dimmed">Region: </Text>
                  <Text transform="uppercase">{t.region}</Text>
                </Group>
              </Stack>
            </Card>)}
        </SimpleGrid>
      </Stack>
    </WithNavBar>
  );
};

export default Teams;

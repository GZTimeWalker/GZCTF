import { FC } from 'react';
import {
  Group,
  Title,
  Text,
  createStyles,
  Divider,
  Avatar,
  AvatarsGroup,
  Badge,
  Card,
  Stack,
  Box,
  useMantineTheme,
} from '@mantine/core';
import { mdiLockOutline } from '@mdi/js';
import Icon from '@mdi/react';
import api, { TeamInfoModel } from '../Api';

const TeamCard: FC<TeamInfoModel> = (team) => {
  const { data: user } = api.account.useAccountProfile({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  });
  const theme = useMantineTheme();

  return (
    <Card shadow="sm">
      <Group align="stretch">
        <Avatar color="cyan" size="lg" radius="md">
          {team.name?.at(0) ?? 'T'}
        </Avatar>
        <Box style={{ flexGrow: 1 }}>
          <Title order={2} align="left">
            {team.name}
          </Title>
          <Text size="md">{team.bio}</Text>
        </Box>
        <Box style={{ height: '100%' }}>
          {user?.activeTeamId === team.id ? (
            <Text transform="uppercase" size="xs" color="yellow">
              Active
            </Text>
          ) : (
            <Text transform="uppercase" size="xs">
              Inactive
            </Text>
          )}
        </Box>
      </Group>
      <Divider my="sm" />
      <Stack spacing="xs">
        <Group spacing="xs" position="apart">
          <Text transform="uppercase" color="dimmed">
            Role:{' '}
          </Text>
          {team.members?.find((m) => m?.captain && m.id == user?.userId) ? (
            <Badge color="brand" size="lg">
              captain
            </Badge>
          ) : (
            <Badge color="gray" size="lg">
              crew
            </Badge>
          )}
        </Group>
        <Group spacing="xs">
          <Text transform="uppercase" color="dimmed">
            MEMBERS:{' '}
          </Text>
          <Box style={{ flexGrow: 1 }}></Box>
          {team.locked && <Icon path={mdiLockOutline} size={1} color={theme.colors.alert[1]} />}
          <AvatarsGroup limit={3} size="md" styles={{
            child: {
              border: 'none',
            }
          }}>
            {team.members &&
              team.members.map((m) => <Avatar key={m.id} src={m.avatar} />)}
          </AvatarsGroup>
        </Group>
      </Stack>
    </Card>
  );
};

export default TeamCard;

import type { NextPage } from 'next';
import { useState } from 'react';
import {
  Stack,
  SimpleGrid,
  Loader,
  Center,
  Group,
  Button,
  Modal,
  TextInput,
  Text,
} from '@mantine/core';
import { showNotification } from '@mantine/notifications';
import { mdiAccountGroup, mdiAccountMultiplePlus, mdiCheck, mdiClose } from '@mdi/js';
import { Icon } from '@mdi/react';
import api from '../Api';
import LogoHeader from '../components/LogoHeader';
import TeamCard from '../components/TeamCard';
import WithNavBar from '../components/WithNavbar';

const Teams: NextPage = () => {
  const { data: teams, error } = api.team.useTeamGetTeamsInfo({
    refreshInterval: 3000,
  });

  const [joinOpened, setJoinOpened] = useState(false);
  const [joinTeamCode, setJoinTeamCode] = useState('');

  const onJoinTeam = () => {
    api.team
      .teamAccept(joinTeamCode)
      .then(() => {
        showNotification({
          color: 'teal',
          title: '加入队伍成功',
          message: '您的队伍信息已更新',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        });
        setJoinOpened(false);
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        });
        setJoinOpened(false);
      });
  };

  return (
    <WithNavBar>
      <Stack>
        <Group position="apart">
          <LogoHeader />
          <Group position="right">
            <Button
              leftIcon={<Icon path={mdiAccountMultiplePlus} size={1} />}
              variant="outline"
              onClick={() => setJoinOpened(true)}
            >
              加入队伍
            </Button>
            <Button leftIcon={<Icon path={mdiAccountGroup} size={1} />} variant="outline">
              创建队伍
            </Button>
          </Group>
        </Group>
        {teams && !error ? (
          <SimpleGrid
            cols={3}
            spacing="lg"
            breakpoints={[
              { maxWidth: 1200, cols: 2, spacing: 'md' },
              { maxWidth: 800, cols: 1, spacing: 'sm' },
            ]}
          >
            {teams.map((t, i) => (
              <TeamCard key={i} {...t} />
            ))}
          </SimpleGrid>
        ) : (
          <Center style={{ width: '100%', height: '100%' }}>
            <Loader />
          </Center>
        )}
      </Stack>
      <Modal opened={joinOpened} centered title="加入已有队伍" onClose={() => setJoinOpened(false)}>
        <Stack>
          <Text size="sm">
            请从队伍创建者处获取队伍邀请码，然后输入邀请码以加入队伍。
            <strong>每个邀请码只能使用一次。</strong>
          </Text>
          <TextInput
            label="邀请码"
            type="text"
            placeholder="team:0:01234567890123456789012345678901"
            style={{ width: '100%' }}
            value={joinTeamCode}
            onChange={(event) => setJoinTeamCode(event.currentTarget.value)}
          />
          <Button fullWidth variant="outline" onClick={onJoinTeam}>
            加入队伍
          </Button>
        </Stack>
      </Modal>
    </WithNavBar>
  );
};

export default Teams;

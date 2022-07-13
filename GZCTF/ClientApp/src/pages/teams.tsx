import type { NextPage } from 'next';
import { useState } from 'react';
import {
  Stack,
  SimpleGrid,
  Loader,
  Center,
  Group,
  UnstyledButton,
  Button,
  Modal,
  TextInput,
  Text
} from '@mantine/core';
import { showNotification } from '@mantine/notifications';
import { mdiAccountGroup, mdiAccountMultiplePlus, mdiCheck, mdiClose } from '@mdi/js';
import { Icon } from '@mdi/react';
import api, { TeamInfoModel } from '../Api';
import LogoHeader from '../components/LogoHeader';
import TeamCard from '../components/TeamCard';
import TeamEditModal from '../components/TeamEditModal';
import WithNavBar from '../components/WithNavbar';

const Teams: NextPage = () => {
  const { data: teams, error } = api.team.useTeamGetTeamsInfo({
    refreshInterval: 3000,
  });

  const [joinOpened, setJoinOpened] = useState(false);
  const [joinTeamCode, setJoinTeamCode] = useState('');
  const [editOpened, setEditOpened] = useState(false);
  const [editTeam, setEditTeam] = useState(null as TeamInfoModel | null);

  const onEditTeam = (id: number) => {
    let cur = teams?.find((team) => team.id === id);
    if (cur) {
      setEditTeam(cur);
      setEditOpened(true);
    } else {
      showNotification({
        color: 'red',
        title: '遇到了问题',
        message: '所请求的队伍不存在',
        icon: <Icon path={mdiClose} size={1} />,
      });
    }
  };

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
            <Button
              leftIcon={<Icon path={mdiAccountGroup} size={1} />}
              variant="outline"
              onClick={() => {
                setEditTeam(null);
                setEditOpened(true);
              }}
            >
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
              <UnstyledButton key={i} onClick={() => onEditTeam(t.id!)}>
                <TeamCard {...t} />
              </UnstyledButton>
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
      <TeamEditModal
        opened={editOpened}
        centered
        title={editTeam ? '编辑队伍' : '创建队伍'}
        onClose={() => setEditOpened(false)}
        team={editTeam}
      />
    </WithNavBar>
  );
};

export default Teams;

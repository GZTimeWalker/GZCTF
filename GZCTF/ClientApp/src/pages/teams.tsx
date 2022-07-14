import type { NextPage } from 'next';
import { useState } from 'react';
import {
  Stack,
  SimpleGrid,
  Loader,
  Center,
  Group,
  Textarea,
  Button,
  Modal,
  TextInput,
  Text,
} from '@mantine/core';
import { showNotification } from '@mantine/notifications';
import { mdiAccountGroup, mdiAccountMultiplePlus, mdiCheck, mdiClose } from '@mdi/js';
import { Icon } from '@mdi/react';
import api, { TeamInfoModel, TeamUpdateModel } from '../Api';
import LogoHeader from '../components/LogoHeader';
import TeamCard from '../components/TeamCard';
import TeamEditModal from '../components/TeamEditModal';
import WithNavBar from '../components/WithNavbar';
import TeamCreateModal from '../components/TeamCreateModal';

const Teams: NextPage = () => {
  const {
    data: teams,
    error,
    mutate,
  } = api.team.useTeamGetTeamsInfo({
    refreshInterval: 3000,
  });

  const { data: user } = api.account.useAccountProfile({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  });

  const [joinOpened, setJoinOpened] = useState(false);
  const [joinTeamCode, setJoinTeamCode] = useState('');

  const [createOpened, setCreateOpened] = useState(false);

  const [editOpened, setEditOpened] = useState(false);
  const [editTeam, setEditTeam] = useState(null as TeamInfoModel | null);

  const [leaveOpened, setLeaveOpened] = useState(false);
  const [leaveTeam, setLeaveTeam] = useState(null as TeamInfoModel | null);

  const ownTeam = teams?.some((t) => t.members?.some((m) => m?.captain && m.id == user?.userId));

  const onEditTeam = (team: TeamInfoModel) => {
    setEditTeam(team);
    setEditOpened(true);
  };

  const onLeaveTeam = (team: TeamInfoModel) => {
    setLeaveTeam(team);
    setLeaveOpened(true);
  };

  const onJoinTeam = () => {
    const parts = joinTeamCode.split(':');

    if (parts.length !== 3 || parts[2].length !== 32) {
      showNotification({
        color: 'red',
        title: '遇到了问题',
        message: '队伍邀请码格式不正确',
        icon: <Icon path={mdiClose} size={1} />,
      });
      return;
    }

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
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        });
      })
      .finally(() => {
        setJoinTeamCode('');
        setJoinOpened(false);
      });
  };

  const onConfirmLeaveTeam = () => {
    if (leaveTeam) {
      api.team
        .teamLeave(leaveTeam.id!)
        .then(() => {
          showNotification({
            color: 'teal',
            title: '退出队伍成功',
            message: '您的队伍信息已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          });
          setLeaveOpened(false);
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '遇到了问题',
            message: `${err.error.title}`,
            icon: <Icon path={mdiClose} size={1} />,
          });
          setLeaveOpened(false);
        });
    }
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
              onClick={() => setCreateOpened(true)}
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
              <TeamCard
                key={i}
                team={t}
                isActive={t.id === user?.activeTeamId}
                isCaptain={t.members?.some((m) => m?.captain && m.id == user?.userId) ?? false}
                onEdit={() => onEditTeam(t)}
                onLeave={() => onLeaveTeam(t)}
              />
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

      <Modal opened={leaveOpened} centered title="离开队伍" onClose={() => setLeaveOpened(false)}>
        <Stack>
          <Text size="sm">你确定要离开 {leaveTeam?.name} 吗？</Text>
          <Button color="red" fullWidth variant="outline" onClick={onConfirmLeaveTeam}>
            确认离开
          </Button>
        </Stack>
      </Modal>

      <TeamCreateModal
        opened={createOpened}
        centered
        title="创建新队伍"
        isOwnTeam={ownTeam ?? false}
        onClose={() => setCreateOpened(false)}
        mutate={mutate}
      />

      <TeamEditModal
        opened={editOpened}
        centered
        title="编辑队伍"
        onClose={() => setEditOpened(false)}
        team={editTeam}
        mutate={mutate}
      />

    </WithNavBar>
  );
};

export default Teams;

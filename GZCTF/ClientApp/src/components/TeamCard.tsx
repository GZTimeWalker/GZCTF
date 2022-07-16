import { FC, useEffect, useRef, useState } from 'react';
import {
  Group,
  Title,
  Text,
  Divider,
  Avatar,
  AvatarsGroup,
  Badge,
  Card,
  Stack,
  Box,
  useMantineTheme,
  ActionIcon,
  Tooltip,
} from '@mantine/core';
import { showNotification } from '@mantine/notifications';
import { mdiLockOutline, mdiPower, mdiCheck, mdiClose } from '@mdi/js';
import Icon from '@mdi/react';
import api, { TeamInfoModel } from '../Api';

interface TeamCardProps {
  team: TeamInfoModel;
  isCaptain: boolean;
  isActive: boolean;
  onEdit: () => void;
  mutateActive: () => void;
}

const TeamCard: FC<TeamCardProps> = (props) => {
  const { team, isCaptain, isActive, onEdit, mutateActive } = props

  const captain = team.members?.filter((m) => m?.captain).at(0);
  const members = team.members?.filter((m) => !m?.captain);

  const theme = useMantineTheme();
  const [cardClickable, setCardClickable] = useState(true);

  const onActive = () => {
    if (isActive) {
      return;
    } else {
      setCardClickable(false);
      api.team
        .teamSetActive(team.id!)
        .then(() => {
          showNotification({
            color: 'teal',
            title: '激活队伍成功',
            message: '您的队伍已经更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          });
          mutateActive();
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
          setCardClickable(true);
        });
    }
  };

  const ref = useRef();
  const [cardSzY, setCardSzY] = useState("180px");
  useEffect(() => {
    setCardSzY(window.getComputedStyle(ref.current).getPropertyValue("height"));
  }, [])

  return (
    <Card
      shadow="sm"
      onClick={() => {
        if (cardClickable) {
          onEdit();
        }
      }}
      sx={(theme) => ({
        cursor: 'pointer',
        transition: 'filter .2s',
        '&:hover': {
          filter: theme.colorScheme === 'dark' ? 'brightness(1.2)' : 'brightness(.97)',
        },
      })}
    >
      <Group align="stretch" style={{ flexWrap: "nowrap", alignItems: "center" }}>
        {isActive &&
          <Avatar color="cyan" size="xl" radius="md" src={team.avatar} style={{ height: cardSzY, width: "auto", aspectRatio: "1 / 1", flexShrink: 0 }}>
            {team.name?.at(0) ?? 'T'}
          </Avatar>
        }
        <Stack style={{ flexGrow: 1 }} ref={ref}>
          <Group align="stretch">
            {!isActive &&
              <Avatar color="cyan" size="lg" radius="md" src={team.avatar}>
                {team.name?.at(0) ?? 'T'}
              </Avatar>
            }
            <Box style={{ flexGrow: 1 }}>
              <Title order={2} align="left">
                {team.name}
              </Title>
              <Text size="md" lineClamp={3}>{team.bio}</Text>
            </Box>
            {!isActive && (
              <Box style={{ height: '100%' }}>
                <Tooltip
                  label={'激活'}
                  styles={(theme) => ({
                    body: {
                      margin: 4,
                      backgroundColor:
                        theme.colorScheme === 'dark'
                          ? theme.colors[theme.primaryColor][8] + '40'
                          : theme.colors[theme.primaryColor][2],
                      color:
                        theme.colorScheme === 'dark'
                          ? theme.colors[theme.primaryColor][4]
                          : theme.colors.gray[8],
                    },
                  })}
                  position="left"
                  transition="pop-bottom-right"
                  color="brand"
                >
                  <ActionIcon
                    size="lg"
                    onMouseEnter={() => setCardClickable(false)}
                    onMouseLeave={() => setCardClickable(true)}
                    onClick={onActive}
                    sx={(theme) => ({
                      '&:hover': {
                        color:
                          theme.colorScheme === 'dark'
                            ? theme.colors[theme.primaryColor][2]
                            : theme.colors[theme.primaryColor][7],
                        backgroundColor:
                          theme.colorScheme === 'dark' ? theme.colors.dark[7] : theme.white,
                      },
                    })}
                  >
                    <Icon path={mdiPower} size={1} />
                  </ActionIcon>
                </Tooltip>
              </Box>
            )}
          </Group>
          <Divider my="xs" />
          <Stack spacing="xs">
            <Group spacing="xs" position="apart">
              <Text transform="uppercase" color="dimmed">
                Role:
              </Text>
              {isCaptain ? (
                <Badge color="yellow" size="lg">
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
                MEMBERS:
              </Text>
              <Box style={{ flexGrow: 1 }}></Box>
              {team.locked && <Icon path={mdiLockOutline} size={1} color={theme.colors.alert[1]} />}
              <AvatarsGroup
                limit={3}
                size="md"
                styles={{
                  child: {
                    border: 'none',
                  },
                }}
              >
                <Avatar src={captain?.avatar} />
                {members && members.map((m) => <Avatar key={m.id} src={m.avatar} />)}
              </AvatarsGroup>
            </Group>
          </Stack>
        </Stack>
      </Group>
    </Card>
  );
};

export default TeamCard;

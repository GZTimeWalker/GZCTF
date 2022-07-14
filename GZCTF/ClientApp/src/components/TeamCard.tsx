import { FC, useState } from 'react';
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
  Button,
  ActionIcon,
  Tooltip,
} from '@mantine/core';
import { mdiLockOutline, mdiPinOffOutline, mdiPinOutline } from '@mdi/js';
import Icon from '@mdi/react';
import api, { TeamInfoModel } from '../Api';

interface TeamCardProps {
  team: TeamInfoModel;
  isCaptain: boolean;
  isActive: boolean;
  onEdit?: () => void;
  onLeave?: () => void;
}

const TeamCard: FC<TeamCardProps> = (props) => {
  const { team, isCaptain, isActive, onEdit, onLeave } = props;
  const theme = useMantineTheme();
  const [cardClickable, setCardClickable] = useState(true);
  const [showAction, setShowAction] = useState(false);
  return (
    <Card shadow="sm" onClick={() => { if (cardClickable) console.log("Card Clicked!") }} sx={(theme) => ({
      cursor: "pointer",
      transition: "filter .2s",
      '&:hover': {
        filter: theme.colorScheme === 'dark' ? "brightness(1.2)": "brightness(.97)"
      },
    })}>
      <Group align="stretch">
        <Avatar color="cyan" size="lg" radius="md" src={team.avatar}>
          {team.name?.at(0) ?? 'T'}
        </Avatar>
        <Box style={{ flexGrow: 1 }}>
          <Title order={2} align="left">
            {team.name}
          </Title>
          <Text size="md">{team.bio}</Text>
        </Box>
        <Box style={{ height: '100%' }}>
          <Tooltip
            label={isActive ? "取消激活" : "激活"}
            position="right"
            color="brand"
          >
            <ActionIcon size="lg" onMouseEnter={() => setCardClickable(false)} onMouseLeave={() => setCardClickable(true)} onClick={() => { console.log("Pin!") }} sx={(theme) => ({
              '&:hover': {
                color: theme.colors.brand[2],
              },
            })}>
              <Icon path={isActive ? mdiPinOffOutline : mdiPinOutline} size={1} />
            </ActionIcon>
          </Tooltip>
        </Box>
      </Group>
      <Divider my="sm" />
      <Stack spacing="xs">
        <Group spacing="xs" position="apart">
          <Text transform="uppercase" color="dimmed">
            Role:
          </Text>
          {isCaptain ? (
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
            {team.members && team.members.map((m) => <Avatar key={m.id} src={m.avatar} />)}
          </AvatarsGroup>
        </Group>
      </Stack>
    </Card>
  );
};

export default TeamCard;

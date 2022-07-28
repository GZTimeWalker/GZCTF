import { FC, useEffect, useRef, useState } from 'react'
import {
  Group,
  Title,
  Text,
  Divider,
  Avatar,
  Badge,
  Card,
  Stack,
  Box,
  useMantineTheme,
  ActionIcon,
  Tooltip,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiLockOutline, mdiPower, mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { TeamInfoModel } from '../Api'
import { showErrorNotification } from '../utils/ApiErrorHandler'

interface TeamCardProps {
  team: TeamInfoModel
  isCaptain: boolean
  isActive: boolean
  onEdit: () => void
  mutateActive?: () => void
}

const TeamCard: FC<TeamCardProps> = (props) => {
  const { team, isCaptain, isActive, onEdit, mutateActive } = props

  const captain = team.members?.filter((m) => m?.captain).at(0)
  const members = team.members?.filter((m) => !m?.captain)

  const theme = useMantineTheme()
  const [cardClickable, setCardClickable] = useState(true)

  const onActive = () => {
    if (isActive) {
      return
    } else {
      setCardClickable(false)
      api.team
        .teamSetActive(team.id!)
        .then(() => {
          showNotification({
            color: 'teal',
            title: '激活队伍成功',
            message: '队伍信息已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          mutateActive && mutateActive()
        })
        .catch(showErrorNotification)
        .finally(() => {
          setCardClickable(true)
        })
    }
  }

  const ref = useRef<HTMLDivElement | null>(null)
  const [cardSzY, setCardSzY] = useState('180px')
  const avatarLimit = isActive ? 8 : 4

  useEffect(() => {
    setCardSzY(window.getComputedStyle(ref.current!).getPropertyValue('height'))
  }, [])

  return (
    <Card
      shadow="sm"
      onClick={() => {
        if (cardClickable) {
          onEdit()
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
      <Group align="stretch" style={{ flexWrap: 'nowrap', alignItems: 'center' }}>
        {isActive && (
          <Avatar
            color="cyan"
            size="xl"
            radius="md"
            src={team.avatar}
            style={{ height: cardSzY, width: 'auto', aspectRatio: '1 / 1', flexShrink: 0 }}
          >
            {team.name?.at(0) ?? 'T'}
          </Avatar>
        )}
        <Stack style={{ flexGrow: 1 }} ref={ref}>
          <Group align="stretch" position="apart">
            {!isActive && (
              <Avatar color="cyan" size="lg" radius="md" src={team.avatar}>
                {team.name?.at(0) ?? 'T'}
              </Avatar>
            )}
            <Stack spacing={0} style={{ width: 'calc(100% - 72px)' }}>
              <Group style={{ width: '100%' }} position="apart">
                <Title order={2} align="left">
                  {team.name}
                </Title>
                {!isActive && (
                  <Box>
                    <Tooltip
                      label={'激活'}
                      styles={(theme) => ({
                        root: {
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
              <Text size="sm" lineClamp={2} style={{ overflow: 'hidden' }}>
                {team.bio}
              </Text>
            </Stack>
          </Group>
          <Divider my="xs" />
          <Stack spacing="xs">
            <Group spacing="xs" position="apart">
              <Text transform="uppercase" color="dimmed">
                个人身份:
              </Text>
              {isCaptain ? (
                <Badge color="yellow" size="lg">
                  队长
                </Badge>
              ) : (
                <Badge color="gray" size="lg">
                  普通队员
                </Badge>
              )}
            </Group>
            <Group spacing="xs">
              <Text transform="uppercase" color="dimmed">
                队员列表:
              </Text>
              <Box style={{ flexGrow: 1 }}></Box>
              {team.locked && (
                <Icon path={mdiLockOutline} size={1} color={theme.colors.orange[1]} />
              )}
              <Tooltip.Group openDelay={300} closeDelay={100}>
                <Avatar.Group
                  spacing="md"
                  styles={{
                    child: {
                      border: 'none',
                    },
                  }}
                >
                  <Tooltip label={captain?.userName} withArrow>
                    <Avatar radius="xl" src={captain?.avatar} />
                  </Tooltip>
                  {members &&
                    members.slice(0, avatarLimit).map((m) => (
                      <Tooltip key={m.id} label={m.userName} withArrow>
                        <Avatar radius="xl" src={m.avatar} />
                      </Tooltip>
                    ))}
                  {members && members.length > avatarLimit && (
                    <Tooltip
                      label={
                        <>
                          {members.slice(avatarLimit).map((m) => (
                            <Text>{m.userName}</Text>
                          ))}
                        </>
                      }
                      withArrow
                    >
                      <Avatar radius="xl">+{members.length - avatarLimit}</Avatar>
                    </Tooltip>
                  )}
                </Avatar.Group>
              </Tooltip.Group>
            </Group>
          </Stack>
        </Stack>
      </Group>
    </Card>
  )
}

export default TeamCard

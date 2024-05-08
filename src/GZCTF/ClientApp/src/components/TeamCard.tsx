import {
  Avatar,
  Badge,
  Box,
  Card,
  Divider,
  Group,
  Stack,
  Text,
  Title,
  Tooltip,
} from '@mantine/core'
import { mdiLockOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { useHoverCardStyles, useTooltipStyles } from '@Utils/ThemeOverride'
import { TeamInfoModel } from '@Api'

interface TeamCardProps {
  team: TeamInfoModel
  isCaptain: boolean
  onEdit: () => void
}

const AVATAR_LIMIT = 5

const TeamCard: FC<TeamCardProps> = (props) => {
  const { team, isCaptain, onEdit } = props

  const captain = team.members?.filter((m) => m?.captain)[0]
  const members = team.members?.filter((m) => !m?.captain)

  const { classes: tooltipClasses, theme } = useTooltipStyles()
  const { classes: cardClasses } = useHoverCardStyles()

  const { t } = useTranslation()

  return (
    <Card shadow="sm" onClick={onEdit} classNames={cardClasses}>
      <Group align="stretch" style={{ flexWrap: 'nowrap', alignItems: 'center' }}>
        <Stack style={{ flexGrow: 1 }}>
          <Group align="stretch" justify="space-between">
            <Avatar alt="avatar" color="cyan" size="lg" radius="md" src={team.avatar}>
              {team.name?.slice(0, 1) ?? 'T'}
            </Avatar>

            <Stack gap={0} w="calc(100% - 72px)">
              <Group w="100%" justify="left">
                <Title order={2} ta="left">
                  {team.name}
                </Title>
              </Group>
              <Text truncate size="sm" lineClamp={2}>
                {team.bio}
              </Text>
            </Stack>
          </Group>
          <Divider my="xs" />
          <Stack gap="xs">
            <Group gap="xs" justify="space-between">
              <Text tt="uppercase" c="dimmed">
                {t('team.label.role')}
              </Text>
              {isCaptain ? (
                <Badge color="yellow" size="lg">
                  {t('team.content.role.captain')}
                </Badge>
              ) : (
                <Badge color="gray" size="lg">
                  {t('team.content.role.member')}
                </Badge>
              )}
            </Group>
            <Group gap="xs">
              <Text tt="uppercase" c="dimmed">
                {t('team.label.members')}
              </Text>
              <Box style={{ flexGrow: 1 }} />
              {team.locked && (
                <Icon path={mdiLockOutline} size={1} color={theme.colors.yellow[6]} />
              )}
              <Tooltip.Group openDelay={300} closeDelay={100}>
                <Avatar.Group spacing="md">
                  <Tooltip label={captain?.userName} withArrow classNames={tooltipClasses}>
                    <Avatar
                      alt="avatar"
                      radius="xl"
                      src={captain?.avatar}
                      style={{
                        border: 'none',
                      }}
                    >
                      {captain?.userName?.slice(0, 1) ?? 'C'}
                    </Avatar>
                  </Tooltip>
                  {members &&
                    members.slice(0, AVATAR_LIMIT).map((m) => (
                      <Tooltip key={m.id} label={m.userName} withArrow classNames={tooltipClasses}>
                        <Avatar
                          alt="avatar"
                          radius="xl"
                          src={m.avatar}
                          style={{
                            border: 'none',
                          }}
                        >
                          {m.userName?.slice(0, 1) ?? 'U'}
                        </Avatar>
                      </Tooltip>
                    ))}
                  {members && members.length > AVATAR_LIMIT && (
                    <Tooltip
                      label={<Text>{members.slice(AVATAR_LIMIT).join(',')}</Text>}
                      withArrow
                      classNames={tooltipClasses}
                    >
                      <Avatar alt="avatar" radius="xl">
                        +{members.length - AVATAR_LIMIT}
                      </Avatar>
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

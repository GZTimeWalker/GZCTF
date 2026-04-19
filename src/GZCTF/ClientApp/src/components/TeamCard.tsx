import { Avatar, Card, Center, Group, Stack, Text, Tooltip } from '@mantine/core'
import { ScrollingText } from '@Components/ScrollingText'
import { mdiLockOutline, mdiCrown } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC } from 'react'
import { useTranslation } from 'react-i18next'
import { useIsMobile } from '@Utils/ThemeOverride'
import { TeamInfoModel } from '@Api'
import misc from '@Styles/Misc.module.css'
import teamCardClasses from '@Styles/TeamCard.module.css'

interface TeamCardProps {
  team: TeamInfoModel
  isCaptain: boolean
  onEdit: () => void
}

export const TeamCard: FC<TeamCardProps> = (props) => {
  const { team, isCaptain, onEdit } = props

  const { t } = useTranslation()
  const isMobile = useIsMobile()

  return (
    <Card
      shadow="md"
      radius="lg"
      onClick={onEdit}
      className={isMobile ? teamCardClasses.cardMobile : teamCardClasses.card}
      classNames={{ root: misc.hoverCard }}
    >
      <Group className={isMobile ? teamCardClasses.contentGroupMobile : teamCardClasses.contentGroup}>
        <Avatar imageProps={{loading:"lazy"}} alt="avatar" size="xl" radius="xl" src={team.avatar}>
          {team.name?.slice(0, 1) ?? 'T'}
        </Avatar>
        <Stack gap={4} className={misc.flexGrow}>
          <Group justify="space-between" align="center" wrap="nowrap">
            <ScrollingText text={team.name ?? ''} size="xl" fw="bold" maw={480} />
            {isCaptain && <Icon path={mdiCrown} size={1} className={teamCardClasses.captainIcon} />}
          </Group>
          <ScrollingText text={team.bio || t('team.placeholder.bio')} size="sm" c="dimmed" maw={520} />
          <Group justify="space-between" align="center">
            <Text size="sm" c="dimmed" tt="uppercase" fw="bold">
              {t('team.label.members')} ({team.members?.length || 0})
            </Text>
            <Avatar.Group className={teamCardClasses.avatarGroup}>
              {team.members?.slice(0, 6).map((m) => (
                <Tooltip key={m.id} label={m.userName} withArrow>
                  <Avatar imageProps={{loading:"lazy"}} alt="avatar" radius="xl" size="md" src={m.avatar}>
                    {m.userName?.slice(0, 1) ?? 'U'}
                  </Avatar>
                </Tooltip>
              ))}
              {team.members && team.members.length > 6 && (
                <Avatar imageProps={{loading:"lazy"}} radius="xl" size="lg">
                  +{team.members.length - 6}
                </Avatar>
              )}
            </Avatar.Group>
          </Group>
        </Stack>
      </Group>
      {team.locked && (
        <Center className={teamCardClasses.lockBadge}>
          <Icon path={mdiLockOutline} size={0.8} color="white" />
        </Center>
      )}
    </Card>
  )
}

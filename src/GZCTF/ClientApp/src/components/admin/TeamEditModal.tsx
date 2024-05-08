import {
  Avatar,
  Button,
  Center,
  Grid,
  Group,
  Modal,
  ModalProps,
  ScrollArea,
  Stack,
  Text,
  Textarea,
  TextInput,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiLockOutline, mdiStar } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { showErrorNotification } from '@Utils/ApiHelper'
import api, { AdminTeamModel, TeamInfoModel } from '@Api'

interface TeamEditModalProps extends ModalProps {
  team: TeamInfoModel
  mutateTeam: (team: TeamInfoModel) => void
}

const TeamEditModal: FC<TeamEditModalProps> = (props) => {
  const { team, mutateTeam, ...modalProps } = props

  const theme = useMantineTheme()
  const [disabled, setDisabled] = useState(false)
  const [activeTeam, setActiveTeam] = useState<TeamInfoModel>(team)
  const [teamInfo, setTeamInfo] = useState<AdminTeamModel>({})

  const { t } = useTranslation()

  useEffect(() => {
    setTeamInfo({ ...team })
    setActiveTeam(team)
  }, [team])

  const onChangeTeamInfo = () => {
    setDisabled(true)
    api.admin
      .adminUpdateTeam(activeTeam.id!, teamInfo)
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('team.notification.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutateTeam({
          ...activeTeam,
          name: teamInfo.name,
          bio: teamInfo.bio,
          locked: teamInfo.locked ?? activeTeam.locked,
        })
        modalProps.onClose()
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <Modal {...modalProps}>
      {/* User Info */}
      <Stack gap="md" m="auto" mt={15}>
        <Grid grow>
          <Grid.Col span={8}>
            <TextInput
              label={
                <Group justify="left" gap="xs">
                  <Text size="sm">{t('team.label.name')}</Text>
                </Group>
              }
              type="text"
              w="100%"
              value={teamInfo.name ?? 'team'}
              disabled={disabled}
              onChange={(event) => setTeamInfo({ ...teamInfo, name: event.target.value })}
            />
          </Grid.Col>
          <Grid.Col span={4}>
            <Center>
              <Avatar alt="avatar" radius="xl" size={70} src={activeTeam.avatar}>
                {activeTeam.name?.slice(0, 1) ?? 'T'}
              </Avatar>
            </Center>
          </Grid.Col>
        </Grid>

        <Textarea
          label={t('team.label.bio')}
          value={teamInfo.bio ?? t('team.placeholder.bio')}
          w="100%"
          disabled={disabled}
          autosize
          minRows={2}
          maxRows={4}
          onChange={(event) => setTeamInfo({ ...teamInfo, bio: event.target.value })}
        />

        <Group justify="left">
          <Text size="sm">{t('team.label.members')}</Text>
          {team.locked && <Icon path={mdiLockOutline} size={0.8} color={theme.colors.yellow[6]} />}
        </Group>
        <ScrollArea h={165} offsetScrollbars>
          <Stack gap="xs">
            {activeTeam.members?.map((user) => (
              <Group justify="space-between">
                <Group justify="left">
                  <Avatar alt="avatar" src={user.avatar} radius="xl">
                    {user.userName?.slice(0, 1) ?? 'U'}
                  </Avatar>
                  <Stack gap={0}>
                    <Text fw={500}>{user.userName}</Text>
                    <Text size="xs" c="dimmed">{`#${user.id?.substring(0, 8)}`}</Text>
                  </Stack>
                </Group>
                <Group justify="right">
                  {user.captain && <Icon path={mdiStar} size={1} color={theme.colors.yellow[4]} />}
                </Group>
              </Group>
            ))}
          </Stack>
        </ScrollArea>

        <Group grow m="auto" w="100%">
          <Button fullWidth disabled={disabled} onClick={onChangeTeamInfo}>
            {t('admin.button.save')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default TeamEditModal

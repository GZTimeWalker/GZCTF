import { FC, useEffect, useState } from 'react'
import {
  Avatar,
  Text,
  Button,
  Center,
  Grid,
  Group,
  Modal,
  ModalProps,
  Stack,
  Textarea,
  TextInput,
  ScrollArea,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiLockOutline, mdiStar } from '@mdi/js'
import { Icon } from '@mdi/react'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import api, { TeamInfoModel, AdminTeamModel } from '@Api'

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
          message: '队伍信息已更新',
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
      .catch(showErrorNotification)
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <Modal {...modalProps}>
      {/* User Info */}
      <Stack spacing="md" m="auto" mt={15}>
        <Grid grow>
          <Grid.Col span={8}>
            <TextInput
              label={
                <Group position="left" spacing="xs">
                  <Text size="sm">队伍名称</Text>
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
          label="队伍签名"
          value={teamInfo.bio ?? ''}
          w="100%"
          disabled={disabled}
          autosize
          minRows={2}
          maxRows={4}
          onChange={(event) => setTeamInfo({ ...teamInfo, bio: event.target.value })}
        />

        <Group position="left">
          <Text size="sm">队员列表</Text>
          {team.locked && <Icon path={mdiLockOutline} size={0.8} color={theme.colors.yellow[6]} />}
        </Group>
        <ScrollArea h={165} offsetScrollbars>
          <Stack spacing="xs">
            {activeTeam.members?.map((user) => (
              <Group position="apart">
                <Group position="left">
                  <Avatar alt="avatar" src={user.avatar} radius="xl">
                    {user.userName?.slice(0, 1) ?? 'U'}
                  </Avatar>
                  <Stack spacing={0}>
                    <Text fw={500}>{user.userName}</Text>
                    <Text size="xs" c="dimmed">{`#${user.id?.substring(0, 8)}`}</Text>
                  </Stack>
                </Group>
                <Group position="right">
                  {user.captain && <Icon path={mdiStar} size={1} color={theme.colors.yellow[4]} />}
                </Group>
              </Group>
            ))}
          </Stack>
        </ScrollArea>

        <Group grow m="auto" w="100%">
          <Button fullWidth disabled={disabled} onClick={onChangeTeamInfo}>
            保存信息
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default TeamEditModal

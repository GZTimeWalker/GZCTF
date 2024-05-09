import {
  ActionIcon,
  Avatar,
  Box,
  Button,
  Center,
  Grid,
  Group,
  Image,
  Modal,
  ModalProps,
  PasswordInput,
  ScrollArea,
  Stack,
  Text,
  Textarea,
  TextInput,
  Tooltip,
  useMantineTheme,
} from '@mantine/core'
import { Dropzone, IMAGE_MIME_TYPE } from '@mantine/dropzone'
import { useClipboard } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { notifications, showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiRefresh, mdiStar } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { showErrorNotification, tryGetErrorMsg } from '@Utils/ApiHelper'
import api, { TeamInfoModel, TeamUserInfoModel } from '@Api'

interface TeamEditModalProps extends ModalProps {
  team: TeamInfoModel | null
  isCaptain: boolean
}

interface TeamMemberInfoProps {
  user: TeamUserInfoModel
  isCaptain: boolean
  onTransferCaptain: (user: TeamUserInfoModel) => void
  onKick: (user: TeamUserInfoModel) => void
}

const TeamMemberInfo: FC<TeamMemberInfoProps> = (props) => {
  const { user, isCaptain, onKick, onTransferCaptain } = props
  const theme = useMantineTheme()
  const [showBtns, setShowBtns] = useState(false)

  const { t } = useTranslation()

  return (
    <Group
      justify="space-between"
      onMouseEnter={() => setShowBtns(true)}
      onMouseLeave={() => setShowBtns(false)}
    >
      <Group justify="left">
        <Avatar alt="avatar" src={user.avatar} radius="xl">
          {user.userName?.slice(0, 1) ?? 'U'}
        </Avatar>
        <Text fw={500}>{user.userName}</Text>
      </Group>
      {isCaptain && showBtns && (
        <Group gap="xs" justify="right">
          <Tooltip label={t('team.label.transfer')}>
            <ActionIcon variant="transparent" onClick={() => onTransferCaptain(user)}>
              <Icon path={mdiStar} size={1} color={theme.colors.yellow[4]} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={t('team.label.kick')}>
            <ActionIcon variant="transparent" onClick={() => onKick(user)}>
              <Icon path={mdiClose} size={1} color={theme.colors.alert[4]} />
            </ActionIcon>
          </Tooltip>
        </Group>
      )}
    </Group>
  )
}

const TeamEditModal: FC<TeamEditModalProps> = (props) => {
  const { team, isCaptain, ...modalProps } = props

  const teamId = team?.id

  const [teamInfo, setTeamInfo] = useState<TeamInfoModel | null>(team)
  const [dropzoneOpened, setDropzoneOpened] = useState(false)
  const [avatarFile, setAvatarFile] = useState<File | null>(null)
  const [inviteCode, setInviteCode] = useState('')
  const [disabled, setDisabled] = useState(false)
  const { data: teams, mutate: mutateTeams } = api.team.useTeamGetTeamsInfo()

  const theme = useMantineTheme()
  const clipboard = useClipboard()
  const captain = teamInfo?.members?.filter((x) => x.captain)[0]
  const crew = teamInfo?.members?.filter((x) => !x.captain)

  const modals = useModals()

  const { t } = useTranslation()

  useEffect(() => {
    setTeamInfo(team)
  }, [team])

  useEffect(() => {
    if (isCaptain && !inviteCode && teamId) {
      api.team.teamInviteCode(teamId).then((code) => {
        setInviteCode(code.data)
      })
    }
  }, [inviteCode, isCaptain, teamId])

  const onConfirmLeaveTeam = () => {
    if (!teamInfo || isCaptain) return

    api.team
      .teamLeave(teamInfo.id!)
      .then(() => {
        showNotification({
          color: 'teal',
          title: t('team.notification.leave.success'),
          message: t('team.notification.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutateTeams(teams?.filter((x) => x.id !== teamInfo?.id))
        props.onClose()
      })
      .catch((e) => showErrorNotification(e, t))
  }

  const onConfirmDisbandTeam = () => {
    if (!teamInfo || !isCaptain) return
    api.team
      .teamDeleteTeam(teamInfo.id!)
      .then(() => {
        showNotification({
          color: 'teal',
          title: t('team.notification.disband.success'),
          message: t('team.notification.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        setInviteCode('')
        setTeamInfo(null)
        mutateTeams(
          teams?.filter((x) => x.id !== teamInfo.id),
          { revalidate: false }
        )
        props.onClose()
      })
      .catch((e) => showErrorNotification(e, t))
  }

  const onTransferCaptain = (userId: string) => {
    if (!teamInfo || !isCaptain) return
    api.team
      .teamTransfer(teamInfo.id!, {
        newCaptainId: userId,
      })
      .then((team) => {
        showNotification({
          color: 'teal',
          title: t('team.notification.transfer.success'),
          message: t('team.notification.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        setTeamInfo(team.data)
        mutateTeams(
          teams?.map((x) => (x.id === teamInfo.id ? team.data : x)),
          {
            revalidate: false,
          }
        )
      })
      .catch((e) => showErrorNotification(e, t))
  }

  const onConfirmKickUser = (userId: string) => {
    api.team
      .teamKickUser(teamInfo?.id!, userId)
      .then((data) => {
        showNotification({
          color: 'teal',
          title: t('team.notification.kick.success'),
          message: t('team.notification.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        setTeamInfo(data.data)
        mutateTeams(
          teams?.map((x) => (x.id === teamInfo?.id ? data.data : x)),
          {
            revalidate: false,
          }
        )
      })
      .catch((e) => showErrorNotification(e, t))
  }

  const onRefreshInviteCode = () => {
    if (!inviteCode) return

    api.team
      .teamUpdateInviteToken(team?.id!)
      .then((data) => {
        setInviteCode(data.data)
        showNotification({
          color: 'teal',
          message: t('team.notification.invite_code.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
      })
      .catch((e) => showErrorNotification(e, t))
  }

  const onChangeAvatar = () => {
    if (!avatarFile || !teamInfo?.id) return
    setDisabled(true)
    notifications.clean()
    showNotification({
      id: 'upload-avatar',
      color: 'orange',
      message: t('common.avatar.uploading'),
      loading: true,
      autoClose: false,
    })

    api.team
      .teamAvatar(teamInfo?.id, {
        file: avatarFile,
      })
      .then((data) => {
        updateNotification({
          id: 'upload-avatar',
          color: 'teal',
          message: t('common.avatar.uploaded'),
          icon: <Icon path={mdiCheck} size={1} />,
          autoClose: true,
          loading: false,
        })
        setAvatarFile(null)
        const newTeamInfo = { ...teamInfo, avatar: data.data }
        setTeamInfo(newTeamInfo)
        mutateTeams(
          teams?.map((x) => (x.id === teamInfo.id ? newTeamInfo : x)),
          {
            revalidate: false,
          }
        )
      })
      .catch((err) => {
        updateNotification({
          id: 'upload-avatar',
          color: 'red',
          title: t('common.avatar.upload_failed'),
          message: tryGetErrorMsg(err, t),
          icon: <Icon path={mdiClose} size={1} />,
          autoClose: true,
          loading: false,
        })
      })
      .finally(() => {
        setDisabled(false)
        setDropzoneOpened(false)
      })
  }

  const onSaveChange = () => {
    if (!teamInfo || !teamInfo?.id) return
    api.team
      .teamUpdateTeam(teamInfo.id, teamInfo)
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('team.notification.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutateTeams(
          teams?.map((x) => (x.id === teamInfo.id ? teamInfo : x)),
          {
            revalidate: false,
          }
        )
      })
      .catch((e) => showErrorNotification(e, t))
  }

  return (
    <Modal
      {...modalProps}
      onClose={() => {
        setDropzoneOpened(false)
        props.onClose()
      }}
    >
      <Stack gap="lg">
        {/* Team Info */}
        <Grid grow>
          <Grid.Col span={8}>
            <TextInput
              label={t('team.label.name')}
              type="text"
              placeholder={team?.name ?? 'ctfteam'}
              w="100%"
              value={teamInfo?.name ?? 'team'}
              disabled={!isCaptain}
              onChange={(event) => setTeamInfo({ ...teamInfo, name: event.target.value })}
            />
          </Grid.Col>
          <Grid.Col span={4}>
            <Center>
              <Avatar
                alt="avatar"
                radius="xl"
                size={70}
                src={teamInfo?.avatar}
                onClick={() => isCaptain && setDropzoneOpened(true)}
              >
                {teamInfo?.name?.slice(0, 1) ?? 'T'}
              </Avatar>
            </Center>
          </Grid.Col>
        </Grid>
        {isCaptain && (
          <PasswordInput
            label={
              <Group gap={3}>
                <Text size="sm">{t('team.label.invite_code')}</Text>
                <ActionIcon size="sm" onClick={onRefreshInviteCode}>
                  <Icon path={mdiRefresh} size={1} />
                </ActionIcon>
              </Group>
            }
            value={inviteCode}
            placeholder="loading..."
            onClick={() => {
              clipboard.copy(inviteCode)
              showNotification({
                color: 'teal',
                message: t('team.notification.invite_code.copied'),
                icon: <Icon path={mdiCheck} size={1} />,
              })
            }}
            readOnly
          />
        )}
        <Textarea
          label={t('team.label.bio')}
          placeholder={teamInfo?.bio ?? t('team.placeholder.bio')}
          value={teamInfo?.bio ?? ''}
          w="100%"
          disabled={!isCaptain}
          autosize
          minRows={2}
          maxRows={4}
          onChange={(event) => setTeamInfo({ ...teamInfo, bio: event.target.value })}
        />
        <Text size="sm">{t('team.label.members')}</Text>
        <ScrollArea h={140} offsetScrollbars>
          <Stack gap="xs">
            {captain && (
              <Group justify="space-between">
                <Group justify="left">
                  <Avatar alt="avatar" src={captain.avatar} radius="xl">
                    {captain.userName?.slice(0, 1) ?? 'C'}
                  </Avatar>
                  <Text fw={500}>{captain.userName}</Text>
                </Group>
                <Icon path={mdiStar} size={1} color={theme.colors.yellow[4]} />
              </Group>
            )}
            {crew &&
              crew.map((user) => (
                <TeamMemberInfo
                  key={user.id}
                  isCaptain={isCaptain}
                  user={user}
                  onTransferCaptain={(user: TeamUserInfoModel) => {
                    modals.openConfirmModal({
                      title: t('team.content.transfer.confirm.title'),
                      children: (
                        <Text size="sm">
                          {t('team.content.transfer.confirm.message', {
                            team: teamInfo?.name,
                            user: user.userName,
                          })}
                        </Text>
                      ),
                      onConfirm: () => onTransferCaptain(user.id!),
                      confirmProps: { color: 'orange' },
                      zIndex: 10000,
                    })
                  }}
                  onKick={(user: TeamUserInfoModel) => {
                    modals.openConfirmModal({
                      title: t('team.content.kick.confirm.title'),
                      children: (
                        <Text size="sm">
                          {t('team.content.kick.confirm.message', {
                            user: user.userName,
                          })}
                        </Text>
                      ),
                      onConfirm: () => onConfirmKickUser(user.id!),
                      confirmProps: { color: 'orange' },
                      zIndex: 10000,
                    })
                  }}
                />
              ))}
          </Stack>
        </ScrollArea>

        <Group grow m="auto" w="100%">
          <Button
            fullWidth
            color="red"
            variant="outline"
            onClick={() => {
              modals.openConfirmModal({
                title: isCaptain
                  ? t('team.content.disband.confirm.title')
                  : t('team.content.leave.confirm.title'),
                children: (
                  <Text size="sm">
                    {isCaptain
                      ? t('team.content.disband.confirm.message', {
                          team: teamInfo?.name,
                        })
                      : t('team.content.leave.confirm.message', {
                          team: teamInfo?.name,
                        })}
                  </Text>
                ),
                onConfirm: isCaptain ? onConfirmDisbandTeam : onConfirmLeaveTeam,
                confirmProps: { color: 'red' },
                zIndex: 10000,
              })
            }}
          >
            {isCaptain ? t('team.button.disband') : t('team.button.leave')}
          </Button>
          <Button fullWidth disabled={!isCaptain} onClick={onSaveChange}>
            {t('team.button.save')}
          </Button>
        </Group>
      </Stack>

      {/* 更新头像浮窗 */}
      <Modal
        opened={dropzoneOpened}
        onClose={() => setDropzoneOpened(false)}
        withCloseButton={false}
        zIndex={1000}
      >
        <Dropzone
          onDrop={(files) => setAvatarFile(files[0])}
          onReject={() => {
            showNotification({
              color: 'red',
              title: t('common.error.file_invalid.title'),
              message: t('common.error.file_invalid.message'),
              icon: <Icon path={mdiClose} size={1} />,
            })
          }}
          m="0 auto 20px auto"
          miw={220}
          mih={220}
          disabled={disabled}
          maxSize={3 * 1024 * 1024}
          accept={IMAGE_MIME_TYPE}
        >
          <Group justify="center" gap="xl" mih={240} style={{ pointerEvents: 'none' }}>
            {avatarFile ? (
              <Image fit="contain" src={URL.createObjectURL(avatarFile)} alt="avatar" />
            ) : (
              <Box>
                <Text size="xl" inline>
                  {t('common.content.drop_zone.content', {
                    type: t('common.content.drop_zone.type.avatar'),
                  })}
                </Text>
                <Text size="sm" c="dimmed" inline mt={7}>
                  {t('common.content.drop_zone.limit')}
                </Text>
              </Box>
            )}
          </Group>
        </Dropzone>
        <Button fullWidth variant="outline" disabled={disabled} onClick={onChangeAvatar}>
          {t('common.avatar.save')}
        </Button>
      </Modal>
    </Modal>
  )
}

export default TeamEditModal

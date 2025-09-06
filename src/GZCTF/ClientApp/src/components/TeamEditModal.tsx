import {
  ActionIcon,
  Avatar,
  Badge,
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
} from '@mantine/core'
import { Dropzone } from '@mantine/dropzone'
import { useClipboard } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { notifications, showNotification, updateNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiRefresh, mdiStar } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { showErrorMsg, tryGetErrorMsg } from '@Utils/Shared'
import { IMAGE_MIME_TYPES } from '@Utils/Shared'
import api, { TeamInfoModel, TeamUserInfoModel } from '@Api'
import misc from '@Styles/Misc.module.css'
import styles from '@Styles/TeamEditModal.module.css'

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
  const [showBtns, setShowBtns] = useState(false)

  const { t } = useTranslation()

  return (
    <Group
      justify="space-between"
      gap={2}
      p="xs"
      className={styles.teamMember}
      onMouseEnter={() => setShowBtns(true)}
      onMouseLeave={() => setShowBtns(false)}
      onClick={() => setShowBtns(!showBtns)}
    >
      <Group justify="left">
        <Avatar alt="avatar" src={user.avatar} radius="xl" size="md">
          {user.userName?.slice(0, 1) ?? 'U'}
        </Avatar>
        <Text fw={500} size="sm">
          {user.userName}
        </Text>
      </Group>
      {isCaptain && showBtns && (
        <Group gap="xs" justify="right">
          <Tooltip label={t('team.label.transfer')}>
            <ActionIcon
              variant="light"
              color="yellow"
              onClick={(e) => {
                e.stopPropagation()
                onTransferCaptain(user)
              }}
            >
              <Icon path={mdiStar} size={0.8} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label={t('team.label.kick')}>
            <ActionIcon
              variant="light"
              color="red"
              onClick={(e) => {
                e.stopPropagation()
                onKick(user)
              }}
            >
              <Icon path={mdiClose} size={0.8} />
            </ActionIcon>
          </Tooltip>
        </Group>
      )}
    </Group>
  )
}

export const TeamEditModal: FC<TeamEditModalProps> = (props) => {
  const { team, isCaptain, ...modalProps } = props

  const teamId = team?.id

  const [teamInfo, setTeamInfo] = useState<TeamInfoModel | null>(team)
  const [dropzoneOpened, setDropzoneOpened] = useState(false)
  const [avatarFile, setAvatarFile] = useState<File | null>(null)
  const [inviteCode, setInviteCode] = useState('')
  const [disabled, setDisabled] = useState(false)
  const { data: teams, mutate: mutateTeams } = api.team.useTeamGetTeamsInfo()

  const clipboard = useClipboard()
  const captain = teamInfo?.members?.filter((x) => x.captain)[0]
  const crew = teamInfo?.members?.filter((x) => !x.captain)

  const modals = useModals()

  const { t } = useTranslation()

  useEffect(() => {
    setTeamInfo(team)
  }, [team])

  useEffect(() => {
    const fetchCode = async () => {
      if (!isCaptain || !teamId || inviteCode) return

      const code = await api.team.teamInviteCode(teamId!)
      setInviteCode(code.data)
    }

    fetchCode()
  }, [inviteCode, isCaptain, teamId])

  const onConfirmLeaveTeam = async () => {
    if (!teamInfo || isCaptain) return

    try {
      await api.team.teamLeave(teamInfo.id!)
      showNotification({
        color: 'teal',
        title: t('team.notification.leave.success'),
        message: t('team.notification.updated'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutateTeams(
        teams?.filter((x) => x.id !== teamInfo.id),
        { revalidate: false }
      )
      setInviteCode('')
      setTeamInfo(null)
      props.onClose()
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  const onConfirmDisbandTeam = async () => {
    if (!teamInfo || !isCaptain) return

    try {
      await api.team.teamDeleteTeam(teamInfo.id!)
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
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  const onTransferCaptain = async (userId: string) => {
    if (!teamInfo || !isCaptain) return

    try {
      await api.team.teamTransfer(teamInfo.id!, {
        newCaptainId: userId,
      })
      showNotification({
        color: 'teal',
        title: t('team.notification.transfer.success'),
        message: t('team.notification.updated'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutateTeams(
        teams?.map((x) => (x.id === teamInfo.id ? teamInfo : x)),
        {
          revalidate: false,
        }
      )
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  const onConfirmKickUser = async (userId: string) => {
    if (!teamInfo?.id || !isCaptain) return

    try {
      await api.team.teamKickUser(teamInfo.id, userId)
      showNotification({
        color: 'teal',
        title: t('team.notification.kick.success'),
        message: t('team.notification.updated'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
      mutateTeams(
        teams?.map((x) => (x.id === teamInfo.id ? teamInfo : x)),
        {
          revalidate: false,
        }
      )
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  const onRefreshInviteCode = async () => {
    if (!inviteCode || !team?.id) return

    try {
      const code = await api.team.teamUpdateInviteToken(team.id)
      setInviteCode(code.data)
      showNotification({
        color: 'teal',
        message: t('team.notification.invite_code.updated'),
        icon: <Icon path={mdiCheck} size={1} />,
      })
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  const onChangeAvatar = async () => {
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

    try {
      const data = await api.team.teamAvatar(teamInfo.id, {
        file: avatarFile,
      })
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
    } catch (err) {
      updateNotification({
        id: 'upload-avatar',
        color: 'red',
        title: t('common.avatar.upload_failed'),
        message: tryGetErrorMsg(err, t),
        icon: <Icon path={mdiClose} size={1} />,
        autoClose: true,
        loading: false,
      })
    } finally {
      setDisabled(false)
      setDropzoneOpened(false)
    }
  }

  const onSaveChange = async () => {
    if (!teamInfo || !teamInfo?.id) return

    try {
      await api.team.teamUpdateTeam(teamInfo.id, teamInfo)
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
    } catch (e) {
      showErrorMsg(e, t)
    }
  }

  return (
    <Modal
      {...modalProps}
      onClose={() => {
        setDropzoneOpened(false)
        props.onClose()
      }}
    >
      <Stack gap="sm">
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
                <Text fw={500} size="sm">
                  {t('team.label.invite_code')}
                </Text>
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
        <Text fw={500} size="sm">
          {t('team.label.members')}
        </Text>
        <ScrollArea h={210} offsetScrollbars>
          <Stack gap="xs">
            {captain && (
              <Group justify="space-between" p="xs" className={styles.captainGroup}>
                <Group justify="left">
                  <Avatar alt="avatar" src={captain.avatar} radius="xl" size="md">
                    {captain.userName?.slice(0, 1) ?? 'C'}
                  </Avatar>
                  <Text fw={500} size="sm">
                    {captain.userName}
                  </Text>
                </Group>
                <Badge color="orange" leftSection={<Icon path={mdiStar} size={0.6} />}>
                  {t('team.content.role.captain')}
                </Badge>
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
                title: isCaptain ? t('team.content.disband.confirm.title') : t('team.content.leave.confirm.title'),
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
      <Modal opened={dropzoneOpened} onClose={() => setDropzoneOpened(false)} withCloseButton={false} zIndex={1000}>
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
          accept={IMAGE_MIME_TYPES}
        >
          <Group justify="center" gap="xl" mih={240} className={misc.n}>
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

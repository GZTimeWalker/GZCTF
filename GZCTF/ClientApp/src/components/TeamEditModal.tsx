import { FC, useEffect, useState } from 'react'
import {
  Avatar,
  Box,
  Button,
  Center,
  Grid,
  Group,
  Image,
  Modal,
  ModalProps,
  Stack,
  Text,
  Textarea,
  TextInput,
  useMantineTheme,
  PasswordInput,
  ActionIcon,
  ScrollArea,
  Tooltip,
} from '@mantine/core'
import { Dropzone } from '@mantine/dropzone'
import { useClipboard } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiRefresh, mdiStar } from '@mdi/js'
import { Icon } from '@mdi/react'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { ACCEPT_IMAGE_MIME_TYPE } from '@Utils/ThemeOverride'
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

  return (
    <Group
      position="apart"
      onMouseEnter={() => setShowBtns(true)}
      onMouseLeave={() => setShowBtns(false)}
    >
      <Group position="left">
        <Avatar src={user.avatar} radius="xl" />
        <Text weight={500}>{user.userName}</Text>
      </Group>
      {isCaptain && showBtns && (
        <Group spacing="xs" position="right">
          <Tooltip label="移交队长">
            <ActionIcon variant="transparent" onClick={() => onTransferCaptain(user)}>
              <Icon path={mdiStar} size={1} color={theme.colors.yellow[4]} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label="移除成员">
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

  const theme = useMantineTheme()
  const clipboard = useClipboard()
  const captain = teamInfo?.members?.filter((x) => x.captain).at(0)
  const crew = teamInfo?.members?.filter((x) => !x.captain)

  const modals = useModals()

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
    if (teamInfo && !isCaptain) {
      api.team
        .teamLeave(teamInfo.id!)
        .then(() => {
          showNotification({
            color: 'teal',
            title: '退出队伍成功',
            message: '队伍信息已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          api.team.mutateTeamGetTeamsInfo()
          props.onClose()
        })
        .catch(showErrorNotification)
    }
  }

  const onConfirmDisbandTeam = () => {
    if (teamInfo && isCaptain) {
      api.team
        .teamDeleteTeam(teamInfo.id!)
        .then(() => {
          showNotification({
            color: 'teal',
            title: '解散队伍成功',
            message: '队伍信息已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          setInviteCode('')
          setTeamInfo(null)
          api.team.mutateTeamGetTeamsInfo()
          props.onClose()
        })
        .catch(showErrorNotification)
    }
  }

  const onTransferCaptain = (userId: string) => {
    if (teamInfo && isCaptain) {
      api.team
        .teamTransfer(teamInfo.id!, {
          newCaptainId: userId,
        })
        .then((team) => {
          showNotification({
            color: 'teal',
            title: '队伍成功',
            message: '队伍信息已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          api.team.mutateTeamGetTeamsInfo()
          setTeamInfo(team.data)
        })
        .catch(showErrorNotification)
    }
  }

  const onConfirmKickUser = (userId: string) => {
    api.team
      .teamKickUser(teamInfo?.id!, userId)
      .then((data) => {
        showNotification({
          color: 'teal',
          title: '踢出成员成功',
          message: '队伍信息已更新',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
        api.team.mutateTeamGetTeamsInfo()
        setTeamInfo(data.data)
      })
      .catch(showErrorNotification)
  }

  const onRefreshInviteCode = () => {
    if (inviteCode) {
      api.team
        .teamUpdateInviteToken(team?.id!)
        .then((data) => {
          setInviteCode(data.data)
          showNotification({
            color: 'teal',
            message: '队伍邀请码已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
        })
        .catch(showErrorNotification)
    }
  }

  const onChangeAvatar = () => {
    if (avatarFile && teamInfo?.id) {
      api.team
        .teamAvatar(teamInfo?.id, {
          file: avatarFile,
        })
        .then((data) => {
          showNotification({
            color: 'teal',
            message: '头像已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          setTeamInfo({ ...teamInfo, avatar: data.data })
          api.team.mutateTeamGetTeamsInfo()
          setAvatarFile(null)
          setDropzoneOpened(false)
        })
        .catch((err) => {
          showErrorNotification(err)
          setDropzoneOpened(false)
        })
    }
  }

  const onSaveChange = () => {
    if (teamInfo && teamInfo?.id) {
      api.team
        .teamUpdateTeam(teamInfo.id, teamInfo)
        .then(() => {
          // Updated TeamInfoModel
          showNotification({
            color: 'teal',
            message: '队伍信息已更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          api.team.mutateTeamGetTeamsInfo()
        })
        .catch(showErrorNotification)
    }
  }

  return (
    <Modal {...modalProps}>
      <Stack spacing="lg">
        {/* Team Info */}
        <Grid grow>
          <Grid.Col span={8}>
            <TextInput
              label="队伍名称"
              type="text"
              placeholder={team?.name ?? 'ctfteam'}
              style={{ width: '100%' }}
              value={teamInfo?.name ?? 'team'}
              disabled={!isCaptain}
              onChange={(event) => setTeamInfo({ ...teamInfo, name: event.target.value })}
            />
          </Grid.Col>
          <Grid.Col span={4}>
            <Center>
              <Avatar
                radius="xl"
                size={70}
                src={teamInfo?.avatar}
                onClick={() => isCaptain && setDropzoneOpened(true)}
              />
            </Center>
          </Grid.Col>
        </Grid>
        {isCaptain && (
          <PasswordInput
            label={
              <Group spacing="xs">
                <Text size="sm">邀请码</Text>
                <ActionIcon
                  size="sm"
                  onClick={onRefreshInviteCode}
                  sx={(theme) => ({
                    margin: '0 0 -0.1rem -0.5rem',
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
                message: '邀请码已复制',
                icon: <Icon path={mdiCheck} size={1} />,
                disallowClose: true,
              })
            }}
            readOnly
          />
        )}

        <Textarea
          label="队伍签名"
          placeholder={teamInfo?.bio ?? '这个人很懒，什么都没有写'}
          value={teamInfo?.bio ?? '这个人很懒，什么都没有写'}
          style={{ width: '100%' }}
          disabled={!isCaptain}
          autosize
          minRows={2}
          maxRows={4}
          onChange={(event) => setTeamInfo({ ...teamInfo, bio: event.target.value })}
        />

        <Text size="sm">队员管理</Text>
        <ScrollArea style={{ height: 140 }} offsetScrollbars>
          <Stack spacing="xs">
            {captain && (
              <Group position="apart">
                <Group position="left">
                  <Avatar src={captain.avatar} radius="xl" />
                  <Text weight={500}>{captain.userName}</Text>
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
                      title: '确认转移队长',
                      centered: true,
                      children: (
                        <Text size="sm">
                          你确定要将队伍 "{teamInfo?.name}" 的队长移交给 "{user.userName}" 吗？
                        </Text>
                      ),
                      onConfirm: () => onTransferCaptain(user.id!),
                      labels: { confirm: '确认', cancel: '取消' },
                      confirmProps: { color: 'orange' },
                      zIndex: 10000,
                    })
                  }}
                  onKick={(user: TeamUserInfoModel) => {
                    modals.openConfirmModal({
                      title: '确认删除',
                      centered: true,
                      children: <Text size="sm">你确定要踢出队员 "{user.userName}" 吗？</Text>,
                      onConfirm: () => onConfirmKickUser(user.id!),
                      labels: { confirm: '确认', cancel: '取消' },
                      confirmProps: { color: 'orange' },
                      zIndex: 10000,
                    })
                  }}
                />
              ))}
          </Stack>
        </ScrollArea>

        <Group grow style={{ margin: 'auto', width: '100%' }}>
          <Button
            fullWidth
            color="red"
            variant="outline"
            onClick={() => {
              modals.openConfirmModal({
                title: isCaptain ? '确认解散' : '确认退出',
                centered: true,
                children: isCaptain ? (
                  <Text size="sm">你确定要解散队伍吗？</Text>
                ) : (
                  <Text size="sm">你确定要退出队伍吗？</Text>
                ),
                onConfirm: isCaptain ? onConfirmDisbandTeam : onConfirmLeaveTeam,
                labels: { confirm: '确认', cancel: '取消' },
                confirmProps: { color: 'red' },
                zIndex: 10000,
              })
            }}
          >
            {isCaptain ? '解散队伍' : '退出队伍'}
          </Button>
          <Button fullWidth disabled={!isCaptain} onClick={onSaveChange}>
            更新信息
          </Button>
        </Group>
      </Stack>

      {/* 更新头像浮窗 */}
      <Modal
        opened={dropzoneOpened}
        onClose={() => setDropzoneOpened(false)}
        centered
        withCloseButton={false}
      >
        <Dropzone
          onDrop={(files) => setAvatarFile(files[0])}
          onReject={() => {
            showNotification({
              color: 'red',
              title: '文件获取失败',
              message: '请检查文件格式和大小',
              icon: <Icon path={mdiClose} size={1} />,
              disallowClose: true,
            })
          }}
          style={{
            margin: '0 auto 20px auto',
            minWidth: '220px',
            minHeight: '220px',
          }}
          maxSize={3 * 1024 * 1024}
          accept={ACCEPT_IMAGE_MIME_TYPE}
        >
          <Group position="center" spacing="xl" style={{ minHeight: 240, pointerEvents: 'none' }}>
            {avatarFile ? (
              <Image fit="contain" src={URL.createObjectURL(avatarFile)} alt="avatar" />
            ) : (
              <Box>
                <Text size="xl" inline>
                  拖放图片或点击此处以选择头像
                </Text>
                <Text size="sm" color="dimmed" inline mt={7}>
                  请选择小于 3MB 的图片
                </Text>
              </Box>
            )}
          </Group>
        </Dropzone>
        <Button fullWidth variant="outline" onClick={onChangeAvatar}>
          更新头像
        </Button>
      </Modal>
    </Modal>
  )
}

export default TeamEditModal

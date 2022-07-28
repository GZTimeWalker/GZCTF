import { FC, useEffect, useState } from 'react'
import {
  Avatar,
  Box,
  Button,
  Center,
  Grid,
  Group,
  Title,
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
} from '@mantine/core'
import { Dropzone } from '@mantine/dropzone'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiCloseCircle, mdiRefresh, mdiCrown } from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { TeamInfoModel, TeamUserInfoModel } from '../Api'
import { showErrorNotification } from '../utils/ApiErrorHandler'

interface TeamEditModalProps extends ModalProps {
  team: TeamInfoModel | null
  isCaptain: boolean
}

interface TeamMemberInfoProps {
  user: TeamUserInfoModel
  isCaptain: boolean
  onKick: (user: TeamUserInfoModel) => void
}

const TeamMemberInfo: FC<TeamMemberInfoProps> = (props) => {
  const { user, isCaptain, onKick } = props

  const [showKick, setShowKick] = useState(false)

  return (
    <Group
      position="apart"
      onMouseEnter={() => setShowKick(true)}
      onMouseLeave={() => setShowKick(false)}
    >
      <Group position="left">
        <Avatar src={user.avatar} radius="xl" />
        <Text>{user.userName}</Text>
      </Group>
      {isCaptain && showKick && <Button onClick={() => onKick(user)}>Kick</Button>}
    </Group>
  )
}

const TeamEditModal: FC<TeamEditModalProps> = (props) => {
  const { team, isCaptain, ...modalProps } = props

  const teamId = team?.id

  const [teamInfo, setTeamInfo] = useState<TeamInfoModel | null>(team)
  const [dropzoneOpened, setDropzoneOpened] = useState(false)
  const [avatarFile, setAvatarFile] = useState<File | null>(null)

  const [leaveOpened, setLeaveOpened] = useState(false)
  const [kickUserOpened, setKickUserOpened] = useState(false)

  const theme = useMantineTheme()
  const clipboard = useClipboard({ timeout: 500 })

  const [inviteCode, setInviteCode] = useState('')
  const [kickUser, setKickUser] = useState<TeamUserInfoModel | null>(null)

  const captain = teamInfo?.members?.filter((x) => x.captain).at(0)
  const crew = teamInfo?.members?.filter((x) => !x.captain)

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
        })
        .catch((err) => {
          showErrorNotification(err)
        })
        .finally(() => {
          setLeaveOpened(false)
          props.onClose()
        })
    }
  }

  const onConfirmKickUser = () => {
    if (kickUser) {
      api.team
        .teamKickUser(teamInfo?.id!, kickUser.id!)
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
        .catch((err) => {
          showErrorNotification(err)
        })
        .finally(() => {
          setKickUserOpened(false)
        })
    }
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
        .catch((err) => {
          showErrorNotification(err)
        })
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
            title: '你改变了一些东西',
            message: '但值得吗？',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          api.team.mutateTeamGetTeamsInfo()
        })
        .catch((err) => {
          showErrorNotification(err)
        })
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
                  <Text>{captain.userName}</Text>
                </Group>
                <Icon path={mdiCrown} size={1} color={theme.colors.yellow[4]} />
              </Group>
            )}
            {crew &&
              crew.map((user) => (
                <TeamMemberInfo
                  key={user.id}
                  isCaptain={isCaptain}
                  user={user}
                  onKick={(user: TeamUserInfoModel) => {
                    setKickUser(user)
                    setKickUserOpened(true)
                  }}
                />
              ))}
          </Stack>
        </ScrollArea>

        <Group grow style={{ margin: 'auto', width: '100%' }}>
          <Button fullWidth color="red" variant="outline" onClick={() => setLeaveOpened(true)}>
            {isCaptain ? '删除队伍' : '退出队伍'}
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
            })
          }}
          style={{
            margin: '0 auto 20px auto',
            minWidth: '220px',
            minHeight: '220px',
          }}
          maxSize={3 * 1024 * 1024}
          accept={['image/png', 'image/gif', 'image/jpeg']}
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

      {/* 删除队伍浮窗 */}
      <Modal
        opened={leaveOpened}
        centered
        title={isCaptain ? '删除队伍' : '离开队伍'}
        onClose={() => setLeaveOpened(false)}
      >
        {isCaptain ? (
          <Stack spacing="lg" p={40} style={{ textAlign: 'center' }}>
            <Center>
              <Icon color={theme.colors.red[7]} path={mdiCloseCircle} size={4} />
            </Center>
            <Title order={3}>暂不允许删除战队</Title>
            <Text>
              为保证数据完整性
              <br />
              删除战队功能已被禁用
            </Text>
          </Stack>
        ) : (
          <Stack>
            <Text size="sm">你确定要离开 {teamInfo?.name} 吗？</Text>
            <Group position="right">
              <Button variant="default" onClick={() => setLeaveOpened(false)}>
                取消
              </Button>
              <Button color="red" onClick={onConfirmLeaveTeam}>
                确认离开
              </Button>
            </Group>
          </Stack>
        )}
      </Modal>

      {/* 删除用户浮窗 */}
      <Modal
        opened={kickUserOpened}
        onClose={() => setKickUserOpened(false)}
        centered
        title="踢出用户"
        withCloseButton={false}
      >
        <Stack>
          <Text size="sm">你确定要踢出 {kickUser?.userName ?? ''} 吗？</Text>
          <Group position="right">
            <Button variant="default" onClick={() => setKickUserOpened(false)}>
              取消
            </Button>
            <Button color="red" onClick={onConfirmKickUser}>
              确认踢出
            </Button>
          </Group>
        </Stack>
      </Modal>
    </Modal>
  )
}

export default TeamEditModal

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
  Select,
  SimpleGrid,
  Stack,
  Text,
  Textarea,
  TextInput,
} from '@mantine/core'
import { Dropzone, DropzoneStatus, IMAGE_MIME_TYPE } from '@mantine/dropzone'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import Icon from '@mdi/react'
import api, { UserInfoModel, UpdateUserInfoModel, Role } from '../../../Api'

interface UserEditModalProps extends ModalProps {
  user: UserInfoModel
  mutateUser: (user: UserInfoModel) => void
}

const dropzoneChildren = (status: DropzoneStatus, file: File | null) => (
  <Group position="center" spacing="xl" style={{ minHeight: 240, pointerEvents: 'none' }}>
    {file ? (
      <Image fit="contain" src={URL.createObjectURL(file)} alt="avatar" />
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
)

const UserEditModal: FC<UserEditModalProps> = (props) => {
  const { user, mutateUser, ...modalProps } = props

  const [dropzoneOpened, setDropzoneOpened] = useState(false)
  const [avatarFile, setAvatarFile] = useState<File | null>(null)
  const [disabled, setDisabled] = useState(false)

  const [avatar, setAvatar] = useState('')
  const [profile, setProfile] = useState<UpdateUserInfoModel>({})

  useEffect(() => {
    setProfile({
      userName: user.userName,
      email: user.email,
      role: user.role,
      bio: user.bio,
      realName: user.realName,
      stdNumber: user.stdNumber,
      phone: user.phone,
    })
    setAvatar(user.avatar ?? '')
  }, [user])

  const onChangeProfile = () => {
    setDisabled(true)
    api.admin
      .adminUpdateUserInfo(user.id!, profile)
      .then(() => {
        showNotification({
          color: 'teal',
          title: '更改成功',
          message: '用户信息已更新',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
        mutateUser({ ...user, ...profile })
        modalProps.onClose()
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
      .finally(() => {
        setDisabled(false)
      })
  }

  const onChangeAvatar = () => {
    if (avatarFile) {
      api.account
        .accountAvatar({
          file: avatarFile,
        })
        .then((res) => {
          showNotification({
            color: 'teal',
            title: '修改头像成功',
            message: '您的头像已经更新',
            icon: <Icon path={mdiCheck} size={1} />,
            disallowClose: true,
          })
          setAvatar(res.data)
          mutateUser({ ...user, avatar })
          setAvatarFile(null)
          setDropzoneOpened(false)
        })
        .catch((err) => {
          showNotification({
            color: 'red',
            title: '遇到了问题',
            message: `${err.error.title}`,
            icon: <Icon path={mdiClose} size={1} />,
          })
          setDropzoneOpened(false)
        })
    }
  }

  return (
    <Modal {...modalProps}>
      {/* User Info */}
      <Stack spacing="md" style={{ margin: 'auto', marginTop: '15px' }}>
        <Grid grow>
          <Grid.Col span={8}>
            <TextInput
              label="用户名"
              type="text"
              style={{ width: '100%' }}
              value={profile.userName ?? 'ctfer'}
              disabled={disabled}
              onChange={(event) => setProfile({ ...profile, userName: event.target.value })}
            />
          </Grid.Col>
          <Grid.Col span={4}>
            <Center>
              <Avatar radius="xl" size={70} src={avatar} onClick={() => setDropzoneOpened(true)} />
            </Center>
          </Grid.Col>
        </Grid>
        <TextInput
          label="邮箱"
          type="email"
          style={{ width: '100%' }}
          value={profile.email ?? 'ctfer@gzti.me'}
          disabled={disabled}
          onChange={(event) => setProfile({ ...profile, email: event.target.value })}
        />
        <TextInput
          label="手机号"
          type="tel"
          style={{ width: '100%' }}
          value={profile.phone ?? ''}
          disabled={disabled}
          onChange={(event) => setProfile({ ...profile, phone: event.target.value })}
        />
        <SimpleGrid cols={2}>
          <TextInput
            label="学工号"
            type="number"
            style={{ width: '100%' }}
            value={profile.stdNumber ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, stdNumber: event.target.value })}
          />
          <TextInput
            label="真实姓名"
            type="text"
            style={{ width: '100%' }}
            value={profile.realName ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, realName: event.target.value })}
          />
        </SimpleGrid>
        <Textarea
          label="描述"
          value={profile.bio ?? '这个人很懒，什么都没有写'}
          style={{ width: '100%' }}
          disabled={disabled}
          autosize
          minRows={2}
          maxRows={4}
          onChange={(event) => setProfile({ ...profile, bio: event.target.value })}
        />
        <Select
          label="用户角色"
          value={profile.role ?? Role.User}
          onChange={(value: Role) => setProfile({ ...profile, role: value })}
          data={Object.entries(Role).map((role) => ({
            value: role[1],
            label: role[0],
          }))}
        />
        <Group grow style={{ margin: 'auto', width: '100%' }}>
          <Button fullWidth disabled={disabled} onClick={onChangeProfile}>
            保存信息
          </Button>
        </Group>
      </Stack>

      {/* Change avatar */}
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
              title: '文件上传失败',
              message: `请重新提交`,
              icon: <Icon path={mdiClose} size={1} />,
            })
          }}
          style={{
            margin: '0 auto 20px auto',
            minWidth: '220px',
            minHeight: '220px',
          }}
          maxSize={3 * 1024 * 1024}
          accept={IMAGE_MIME_TYPE}
        >
          {(status) => dropzoneChildren(status, avatarFile)}
        </Dropzone>
        <Button fullWidth variant="outline" disabled={disabled} onClick={onChangeAvatar}>
          修改头像
        </Button>
      </Modal>
    </Modal>
  )
}

export default UserEditModal

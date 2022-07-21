import { FC, useEffect, useState } from 'react'
import {
  Avatar,
  Box,
  Button,
  Center,
  Grid,
  Group,
  Image,
  InputWrapper,
  Modal,
  ModalProps,
  SegmentedControl,
  SimpleGrid,
  Stack,
  Text,
  Textarea,
  TextInput,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose } from '@mdi/js'
import Icon from '@mdi/react'
import api, { UserInfoModel, UpdateUserInfoModel, Role } from '../../Api'

export const RoleColorMap = new Map<Role, string>([
  [Role.Admin, 'blue'],
  [Role.User, 'brand'],
  [Role.Monitor, 'yellow'],
  [Role.Banned, 'red'],
])

interface UserEditModalProps extends ModalProps {
  user: UserInfoModel
  mutateUser: (user: UserInfoModel) => void
}

const UserEditModal: FC<UserEditModalProps> = (props) => {
  const { user, mutateUser, ...modalProps } = props

  const [disabled, setDisabled] = useState(false)

  const [activeUser, setActiveUser] = useState<UserInfoModel>(user)
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
    setActiveUser(user)
  }, [user])

  const onChangeProfile = () => {
    setDisabled(true)
    api.admin
      .adminUpdateUserInfo(activeUser.id!, profile)
      .then(() => {
        showNotification({
          color: 'teal',
          title: '更改成功',
          message: '用户信息已更新',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
        mutateUser({ ...activeUser, ...profile })
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
              <Avatar radius="xl" size={70} src={activeUser.avatar} />
            </Center>
          </Grid.Col>
        </Grid>
        <InputWrapper label="用户角色">
          <SegmentedControl
            fullWidth
            disabled={disabled}
            color={RoleColorMap.get(profile.role ?? Role.User)}
            value={profile.role ?? Role.User}
            onChange={(value: Role) => setProfile({ ...profile, role: value })}
            data={Object.entries(Role).map((role) => ({
              value: role[1],
              label: role[0],
            }))}
          />
        </InputWrapper>
        <SimpleGrid cols={2}>
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
        <Group grow style={{ margin: 'auto', width: '100%' }}>
          <Button fullWidth disabled={disabled} onClick={onChangeProfile}>
            保存信息
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default UserEditModal
